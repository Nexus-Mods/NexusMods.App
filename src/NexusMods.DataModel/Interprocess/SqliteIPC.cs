using System.Diagnostics;
using System.Reactive.Subjects;
using System.Text.Json;
using DynamicData;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Interprocess.Jobs;
using NexusMods.DataModel.Interprocess.Messages;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Paths;

namespace NexusMods.DataModel.Interprocess;

/// <summary>
/// Fairly simple interprocess communication system using Sqlite. This is not intended to be a high performance system,
/// but rather a simple way to communicate between processes. Messages are stored in a Sqlite database and read by a
/// worker task. The worker task will read all messages from the database, poll for new messages. The worker will pause
/// periodically and check the file size and last modified time of the database file. If it detects that the file has
/// changed, it will re-poll the database for new messages. This method of polling allows for fairly simple change detection
/// without having to run a SQL query every second.
/// </summary>
// ReSharper disable once InconsistentNaming
public class SqliteIPC : IDisposable, IInterprocessJobManager
{
    private static readonly TimeSpan RetentionTime = TimeSpan.FromSeconds(10); // Keep messages for 10 seconds
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(10); // Cleanup every 10 minutes
    private static readonly int CleanupJitter = 2000; // Jitter cleanup by up to 2 second
    private static readonly TimeSpan ShortPollInterval = TimeSpan.FromMilliseconds(100); // Poll every 100ms
    private static readonly TimeSpan LongPollInterval = TimeSpan.FromSeconds(10); // Poll every 10s

    private readonly Subject<(string Queue, byte[] Message)> _subject = new();
    private readonly CancellationTokenSource _shutdownToken;
    private readonly ILogger<SqliteIPC> _logger;
    private readonly ISharedArray _syncArray;

    private SourceCache<IInterprocessJob, JobId> _jobs = new(x => x.JobId);
    private readonly ObjectPool<SqliteConnection> _pool;
    private readonly ConnectionPoolPolicy _poolPolicy;

    private bool _isDisposed;
    private readonly JsonSerializerOptions _jsonSettings;
    private readonly ObjectPoolDisposable<SqliteConnection> _globalHandle;

    /// <summary>
    /// Allows you to subscribe to newly incoming IPC messages.
    /// </summary>
    public IObservable<(string Queue, byte[] Message)> Messages => _subject;

    /// <inheritdoc />
    public IObservable<IChangeSet<IInterprocessJob, JobId>> Jobs => _jobs.Connect();

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="logger">Allows for logging of messages.</param>
    /// <param name="settings">Datamodel settings.</param>
    /// <param name="jsonSettings">JSON serializer settings</param>
    public SqliteIPC(ILogger<SqliteIPC> logger, IDataModelSettings settings, JsonSerializerOptions jsonSettings)
    {
        _logger = logger;
        var storePath = settings.IpcDataStoreFilePath.ToAbsolutePath();

        string connectionString;
        if (settings.UseInMemoryDataModel)
        {
            var id = Guid.NewGuid().ToString();
            connectionString = string.Intern($"Data Source={id};Mode=Memory;Cache=Shared");
            _syncArray = new SingleProcessSharedArray(2);
        }
        else
        {
            var syncPath = storePath.AppendExtension(new Extension(".sync"));
            _syncArray = new MultiProcessSharedArray(syncPath, 2);
            connectionString = string.Intern($"Data Source={storePath}");
        }

        connectionString = string.Intern(connectionString);

        _poolPolicy = new ConnectionPoolPolicy(connectionString);
        _pool = ObjectPool.Create(_poolPolicy);

        // We do this so that while the app is running we never fully close the DB, this is needed
        // if we're using a in-memory store, as closing the final connection will delete the DB.
        _globalHandle = _pool.RentDisposable();

        _jsonSettings = jsonSettings;

        EnsureTables();

        _shutdownToken = new CancellationTokenSource();
        var startId = GetStartId();
        Task.Run(() => ReaderLoop(startId, _shutdownToken.Token));
        Task.Run(() => CleanupLoop(_shutdownToken.Token));
    }

    private async Task CleanupLoop(CancellationToken token)
    {
        // Wait a bit so a bunch of CLI processes don't all try to clean up at the same time.
        await Task.Delay(Random.Shared.Next(CleanupJitter), token);
        while (!token.IsCancellationRequested)
        {
            await CleanupOnce(token);
            await Task.Delay(CleanupInterval + TimeSpan.FromMilliseconds(Random.Shared.Next(CleanupJitter)), token);
        }
    }

    /// <summary>
    /// Cleanup any old messages left in the queue. This is run automatically, but can be called manually if needed.
    /// </summary>
    /// <param name="token"></param>
    public async Task CleanupOnce(CancellationToken token)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SqliteIPC));

        var oldTime = DateTime.UtcNow - RetentionTime;

        _logger.LogTrace("Cleaning up old IPC messages");

        using var conn = _pool.RentDisposable();
        await using var cmd = conn.Value.CreateCommand();
        cmd.CommandText = "DELETE from Ipc WHERE TimeStamp < @timestamp";

        cmd.Parameters.AddWithValue("@timestamp", oldTime.ToFileTimeUtc());
        await cmd.ExecuteNonQueryAsync(token);

        foreach (var job in _jobs.Items)
        {
            try
            {
                Process.GetProcessById((int)job.ProcessId.Value);
            }
            catch (ArgumentException)
            {
                _logger.LogInformation("Removing job {JobId} because the process {ProcessId} no longer exists", job.JobId, job.ProcessId);
                EndJob(job.JobId);
            }
        }
        UpdateJobTimestamp();
    }

    private async Task ReaderLoop(long lastId, CancellationToken shutdownTokenToken)
    {
        var lastJobTimestamp = (long)_syncArray.Get(1);
        while (!shutdownTokenToken.IsCancellationRequested)
        {
            lastId = ProcessMessages(lastId);

            ProcessJobs();

            var elapsed = DateTime.UtcNow;
            while (!shutdownTokenToken.IsCancellationRequested)
            {
                if (lastId < (long)_syncArray.Get(0))
                    break;

                var jobTimeStamp = (long)_syncArray.Get(1);
                if (jobTimeStamp > lastJobTimestamp)
                {
                    lastJobTimestamp = jobTimeStamp;
                    break;
                }

                await Task.Delay(ShortPollInterval, shutdownTokenToken);

                if (DateTime.UtcNow - elapsed > LongPollInterval)
                    break;
            }
        }
    }

    private long GetStartId()
    {
        using var conn = _pool.RentDisposable();
        using var cmd = conn.Value.CreateCommand();
        cmd.CommandText = "SELECT MAX(Id) FROM Ipc";
        // Subtract 1 second to ensure we don't miss any messages that were written in the last second.
        cmd.Parameters.AddWithValue("@current", DateTime.UtcNow.ToFileTimeUtc());
        var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            return reader.IsDBNull(0) ? (long)_syncArray.Get(0) : reader.GetInt64(0);
        }

        return 0L;
    }

    private long ProcessMessages(long lastId)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SqliteIPC));

        try
        {
            using var conn = _pool.RentDisposable();
            using var cmd = conn.Value.CreateCommand();
            cmd.CommandText = "SELECT Id, Queue, Data FROM Ipc WHERE Id > @lastId";
            cmd.Parameters.AddWithValue("@lastId", lastId);
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lastId = long.Max(lastId, reader.GetInt64(0));
                var queue = reader.GetString(1);
                var size = reader.GetBytes(2, 0, null, 0, 0);
                var bytes = new byte[size];
                reader.GetBytes(2, 0, bytes, 0, bytes.Length);
                _subject.OnNext((queue, bytes));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process messages after {LastId}", lastId);
        }

        return lastId;
    }

    private void EnsureTables()
    {
        using var conn = _pool.RentDisposable();
        using (var pragma = conn.Value.CreateCommand())
        {
            pragma.CommandText = "PRAGMA journal_mode = WAL";
            pragma.ExecuteNonQuery();
        }

        using var cmd = conn.Value.CreateCommand();
        cmd.CommandText = "CREATE TABLE IF NOT EXISTS Ipc (Id INTEGER PRIMARY KEY AUTOINCREMENT, Queue VARCHAR, Data BLOB, TimeStamp INTEGER)";
        cmd.ExecuteNonQuery();

        using var cmd2 = conn.Value.CreateCommand();
        cmd2.CommandText = "CREATE TABLE IF NOT EXISTS Jobs (JobId BLOB PRIMARY KEY, ProcessId INTEGER, Progress REAL, StartTime INTEGER, Data BLOB)";
        cmd2.ExecuteNonQuery();
    }

    private void ProcessJobs()
    {
        try
        {
            _logger.ProcessingJobs();
            using var conn = _pool.RentDisposable();
            using var cmd = conn.Value.CreateCommand();
            cmd.CommandText = "SELECT JobId, ProcessId, Progress, StartTime, Data FROM Jobs";
            var reader = cmd.ExecuteReader();

            var seen = new HashSet<JobId>();
            _jobs.Edit(editable =>
            {
                while (reader.Read())
                {
                    var idSize = reader.GetBytes(0, 0, null, 0, 0);
                    var idBytes = new byte[idSize];
                    reader.GetBytes(0, 0, idBytes, 0, idBytes.Length);

                    var jobId = JobId.From(new Guid(idBytes));

                    var progress = new Percent(reader.GetDouble(2));

                    seen.Add(jobId);
                    var item = editable.Lookup(jobId);
                    if (item.HasValue)
                    {
                        if (item.Value.Progress != progress)
                            item.Value.Progress = progress;

                        _logger.JobProgress(jobId, progress);
                        continue;
                    }

                    _logger.NewJob(jobId);
                    var processId = Interprocess.Jobs.ProcessId.From((uint)reader.GetInt64(1));
                    var startTime =
                        DateTime.FromFileTimeUtc(reader.GetInt64(3));

                    var entity =
                        JsonSerializer.Deserialize<Entity>(reader.GetBlob(4),
                            _jsonSettings)!;

                    var newJob = new InterprocessJob(jobId, this, processId, startTime, progress, entity);
                    editable.AddOrUpdate(newJob);
                }

                foreach (var key in editable.Keys)
                {
                    if (seen.Contains(key))
                        continue;

                    _logger.RemovingJob(key);
                    editable.Remove(key);
                }

                _logger.DoneProcessing();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process jobs");
        }
    }

    /// <summary>
    /// Send a message to the queue.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="message"></param>
    public void Send(string queue, ReadOnlySpan<byte> message)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SqliteIPC));

        try
        {
            _logger.SendingByteMessageToQueue(Size.FromLong(message.Length), queue);
            using var conn = _pool.RentDisposable();
            using var cmd = conn.Value.CreateCommand();
            cmd.CommandText = "INSERT INTO Ipc (Queue, Data, TimeStamp) VALUES (@queue, @data, @timestamp); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@queue", queue);
            cmd.Parameters.AddWithValue("@data", message.ToArray());
            cmd.Parameters.AddWithValue("@timestamp",DateTime.UtcNow.ToFileTimeUtc());
            var lastId = (ulong)((long?)cmd.ExecuteScalar()!).Value;
            var prevId = _syncArray.Get(0);
            while (true)
            {
                if (prevId >= lastId)
                    break;
                if (_syncArray.CompareAndSwap(0, prevId, lastId))
                    break;
                prevId = _syncArray.Get(0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to queue {Queue}", queue);
        }
    }

    /// <summary>
    /// Create a new job.
    /// </summary>
    /// <param name="job"></param>
    /// <exception cref="ObjectDisposedException"></exception>
    public void CreateJob<T>(IInterprocessJob job) where T : Entity
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SqliteIPC));

        try
        {
            _logger.CreatingJob(job.JobId, job.Payload.GetType());
            using var conn = _pool.RentDisposable();
            using var cmd = conn.Value.CreateCommand();
            cmd.CommandText = "INSERT INTO Jobs (JobId, ProcessId, Progress, StartTime, Data) " +
                              "VALUES (@jobId, @processId, @progress, @startTime, @data);";

            cmd.Parameters.AddWithValue("@jobId", job.JobId.Value.ToByteArray());
            cmd.Parameters.AddWithValue("@processId", job.ProcessId.Value);
            cmd.Parameters.AddWithValue("@progress", job.Progress.Value);
            cmd.Parameters.AddWithValue("@startTime", job.StartTime.ToFileTimeUtc());

            var ms = new MemoryStream();
            JsonSerializer.Serialize(ms, (T)job.Payload, _jsonSettings);
            ms.Position = 0;
            cmd.Parameters.AddWithValue("@data", ms.ToArray());
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create job {JobId} of type {JobType}", job.JobId, job.GetType().Name);
        }

        UpdateJobTimestamp();
    }

    private void UpdateJobTimestamp()
    {
        var prevTimeStamp = _syncArray.Get(1);
        while (true)
        {
            if (_syncArray.CompareAndSwap(1, prevTimeStamp, (ulong)DateTime.UtcNow.ToFileTimeUtc()))
                break;
            prevTimeStamp = _syncArray.Get(1);
        }
    }

    /// <inheritdoc />
    public void EndJob(JobId job)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TemporaryFileManager));

        _logger.DeletingJob(job);
        {
            using var conn = _pool.RentDisposable();
            using var cmd = conn.Value.CreateCommand();
            cmd.CommandText = "DELETE FROM Jobs WHERE JobId = @jobId";
            cmd.Parameters.AddWithValue("@jobId", job.Value.ToByteArray());
            cmd.ExecuteNonQuery();
        }

        UpdateJobTimestamp();
    }

    /// <inheritdoc />
    public void UpdateProgress(JobId jobId, Percent value)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TemporaryFileManager));

        _logger.UpdatingJobProgress(jobId, value);

        {
            using var conn = _pool.RentDisposable();
            using var cmd = conn.Value.CreateCommand();
            cmd.CommandText = "UPDATE Jobs SET Progress = @progress WHERE JobId = @jobId";
            cmd.Parameters.AddWithValue("@progress", value.Value);
            cmd.Parameters.AddWithValue("@jobId", jobId.Value.ToByteArray());
            cmd.ExecuteNonQuery();
        }

        UpdateJobTimestamp();
    }

    /// <summary>
    /// Dispose of the IPC connection.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <c>true</c> to release both managed and unmanaged resources;
    /// <c>false</c> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        if (disposing)
        {
            _globalHandle.Dispose();
            _shutdownToken.Cancel();
            _subject.Dispose();
            _syncArray.Dispose();
            _jobs.Dispose();

            if (_pool is IDisposable disposable)
                disposable.Dispose();
            _poolPolicy.Dispose();
        }

        _isDisposed = true;
    }
}
