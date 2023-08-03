using System.Diagnostics;
using System.Reactive.Subjects;
using System.Text.Json;
using DynamicData;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Interprocess.Jobs;
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
    private static readonly TimeSpan SqliteDefaultTimeout = TimeSpan.FromMilliseconds(150);

    private static readonly TimeSpan RetentionTime = TimeSpan.FromSeconds(10); // Keep messages for 10 seconds
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(10); // Cleanup every 10 minutes
    private const int CleanupJitter = 2000; // Jitter cleanup by up to 2 second

    private static readonly TimeSpan ReaderLoopInterval = SqliteDefaultTimeout + TimeSpan.FromMilliseconds(50);

    private bool _isDisposed;

    private readonly ILogger<SqliteIPC> _logger;
    private readonly CancellationTokenSource _shutdownToken;
    private readonly JsonSerializerOptions _jsonSettings;

    private readonly string _connectionString;
    private readonly ISharedArray _syncArray;
    private readonly SqliteConnection? _globalConnection;

    private readonly Subject<(string Queue, byte[] Message)> _subject = new();
    private readonly SourceCache<IInterprocessJob, JobId> _jobs = new(x => x.JobId);

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
        _jsonSettings = jsonSettings;
        _shutdownToken = new CancellationTokenSource();

        const int syncSlots = 2;

        var builder = new SqliteConnectionStringBuilder
        {
            Pooling = true,
            ForeignKeys = true
        };

        if (settings.UseInMemoryDataModel)
        {
            builder.DataSource = Guid.NewGuid().ToString();
            builder.Mode = SqliteOpenMode.Memory;
            builder.Cache = SqliteCacheMode.Shared;

            _syncArray = new SingleProcessSharedArray(syncSlots);
        }
        else
        {
            var storePath = settings.IpcDataStoreFilePath.ToAbsolutePath();
            builder.DataSource = storePath.ToString();
            builder.Mode = SqliteOpenMode.ReadWriteCreate;
            builder.Cache = SqliteCacheMode.Private;

            var syncPath = storePath.AppendExtension(new Extension(".sync"));
            _syncArray = new MultiProcessSharedArray(syncPath, syncSlots);
        }

        _connectionString = string.Intern(builder.ConnectionString);

        if (settings.UseInMemoryDataModel)
        {
            // Enabling shared-cache for an in-memory database allows two or more database connections in the
            // same process to have access to the same in-memory database. An in-memory database in shared cache is
            // automatically deleted and memory is reclaimed when the last connection to that database closes.
            // Source: https://www.sqlite.org/sharedcache.html#shared_cache_and_in_memory_databases

            // As such, we need a "global" connection when using an in-memory database to prevent the database
            // from being deleted before we close the app or finish the tests.
            _globalConnection = CreateConnection();
        }

        EnsureTables();

        var startId = GetStartId();
        Task.Run(() => ReaderLoop(startId, _shutdownToken.Token));
        Task.Run(() => CleanupLoop(_shutdownToken.Token));
    }

    private SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        return connection;
    }

    private async Task CleanupLoop(CancellationToken token)
    {
        // Wait a bit so a bunch of CLI processes don't all try to clean up at the same time.
        await Task.Delay(Random.Shared.Next(CleanupJitter), token);
        while (!token.IsCancellationRequested)
        {
            CleanupOnce();
            await Task.Delay(CleanupInterval + TimeSpan.FromMilliseconds(Random.Shared.Next(CleanupJitter)), token);
        }
    }

    /// <summary>
    /// Cleanup any old messages left in the queue. This is run automatically, but can be called manually if needed.
    /// </summary>
    public void CleanupOnce()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var oldTime = DateTime.UtcNow - RetentionTime;
        _logger.LogTrace("Cleaning up old IPC messages");

        using (var connection = CreateConnection())
        using (var transaction = connection.BeginTransaction())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "DELETE from Ipc WHERE TimeStamp < @timestamp";
            command.Parameters.AddWithValue("@timestamp", oldTime.ToFileTimeUtc());
            command.ExecuteNonQuery();
            transaction.Commit();
        }

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
        var sw = new Stopwatch();
        while (!shutdownTokenToken.IsCancellationRequested)
        {
            sw.Restart();

            lastId = ProcessMessages(lastId);
            ProcessJobs();

            var elapsed = sw.Elapsed;
            if (elapsed > SqliteDefaultTimeout)
            {
                _logger.LogDebug("ReaderLoop was locked by Sqlite for {}ms", elapsed.TotalMilliseconds);
                continue;
            }

            await Task.Delay(ReaderLoopInterval - elapsed, shutdownTokenToken);
        }
    }

    private long GetStartId()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT MAX(Id) FROM Ipc";

        var result = command.ExecuteScalar();
        return result == DBNull.Value ? (long)_syncArray.Get(0) : Convert.ToInt64(result);
    }

    private readonly Queue<(string, byte[])> _updates = new(256);
    private long ProcessMessages(long lastId)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        try
        {
            using var connection = CreateConnection();
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT Id, Queue, Data FROM Ipc WHERE Id > @lastId";
            command.Parameters.AddWithValue("@lastId", lastId);

            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lastId = long.Max(lastId, reader.GetInt64(0));

                var queue = reader.GetString(1);

                var size = reader.GetBytes(2, 0, null, 0, 0);
                var bytes = new byte[size];
                reader.GetBytes(2, 0, bytes, 0, bytes.Length);

                _updates.Enqueue((queue, bytes));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process messages after {LastId}", lastId);
        }

        while (_updates.TryDequeue(out var update))
        {
            _subject.OnNext(update);
        }

        return lastId;
    }

    private void EnsureTables()
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        using var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = "PRAGMA journal_mode = WAL";
        pragmaCommand.ExecuteNonQuery();

        using var ipcTableCommand = connection.CreateCommand();
        ipcTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS Ipc (Id INTEGER PRIMARY KEY ASC AUTOINCREMENT, Queue TEXT, Data BLOB, TimeStamp INTEGER)";
        ipcTableCommand.ExecuteNonQuery();

        using var jobsTableCommand = connection.CreateCommand();
        jobsTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS Jobs (JobId BLOB PRIMARY KEY, ProcessId INTEGER, Progress REAL, StartTime INTEGER, Data BLOB)";
        jobsTableCommand.ExecuteNonQuery();

        transaction.Commit();
    }

    private readonly Queue<(JobId, Percent)> _updatedJobs = new(128);
    private readonly Queue<InterprocessJob> _newJobs = new(64);
    private void ProcessJobs()
    {
        _logger.ProcessingJobs();

        var knownJobs = _jobs.Keys.ToArray();
        Array.Sort(knownJobs);

        try
        {
            using var connection = CreateConnection();
            using var _ = connection.BeginTransaction();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT JobId, ProcessId, Progress, StartTime, Data FROM Jobs";
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var jobId = JobId.From(new Guid(reader.GetBlob(0)));
                var isKnownJob = Array.BinarySearch(knownJobs, jobId) >= 0;

                var progress = new Percent(reader.GetDouble(2));
                if (isKnownJob)
                {
                    _updatedJobs.Enqueue((jobId, progress));
                    continue;
                }

                var processId = ProcessId.From((uint)reader.GetInt64(1));
                var startTime = DateTime.FromFileTimeUtc(reader.GetInt64(3));
                var entity = JsonSerializer.Deserialize<Entity>(reader.GetBlob(4), _jsonSettings)!;

                var newJob = new InterprocessJob(jobId, this, processId, startTime, progress, entity);
                _newJobs.Enqueue(newJob);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read jobs from the database");
        }

        _jobs.Edit(editable =>
        {
            var seen = new HashSet<JobId>();

            while (_updatedJobs.TryDequeue(out var tuple))
            {
                var (jobId, progress) = tuple;
                seen.Add(jobId);

                var item = editable.Lookup(jobId);
                if (!item.HasValue || item.Value.Progress == progress) continue;

                item.Value.Progress = progress;
                _logger.JobProgress(jobId, progress);
                editable.AddOrUpdate(item.Value);
            }

            while (_newJobs.TryDequeue(out var newJob))
            {
                seen.Add(newJob.JobId);
                editable.AddOrUpdate(newJob);
            }

            foreach (var key in editable.Keys)
            {
                if (seen.Contains(key)) continue;
                _logger.RemovingJob(key);
                editable.Remove(key);
            }

            _logger.DoneProcessing();
        });
    }

    /// <summary>
    /// Send a message to the queue.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="message"></param>
    public void Send(string queue, ReadOnlySpan<byte> message)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        ulong lastId = 0;

        try
        {
            using var connection = CreateConnection();
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();

            command.CommandText = "INSERT INTO Ipc (Queue, Data, TimeStamp) VALUES (@queue, @data, @timestamp); SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("@queue", queue);
            command.Parameters.AddWithValue("@data", message.ToArray());
            command.Parameters.AddWithValue("@timestamp",DateTime.UtcNow.ToFileTimeUtc());

            var result = command.ExecuteScalar();
            transaction.Commit();

            lastId = result == DBNull.Value ? 0 : Convert.ToUInt64(result);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while inserting a new message");
        }

        var prevId = _syncArray.Get(0);
        while (true)
        {
            if (prevId >= lastId) break;
            if (_syncArray.CompareAndSwap(0, prevId, lastId)) break;
            prevId = _syncArray.Get(0);
        }
    }

    /// <summary>
    /// Create a new job.
    /// </summary>
    /// <param name="job"></param>
    /// <exception cref="ObjectDisposedException"></exception>
    public void CreateJob<T>(IInterprocessJob job) where T : Entity
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        _logger.CreatingJob(job.JobId, job.Payload.GetType());

        try
        {
            var data = JsonSerializer.SerializeToUtf8Bytes((T)job.Payload, _jsonSettings);
            var jobIdBytes = job.JobId.Value.ToByteArray();

            using var connection = CreateConnection();
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();

            command.CommandText = "INSERT INTO Jobs (JobId, ProcessId, Progress, StartTime, Data) VALUES (@jobId, @processId, @progress, @startTime, @data);";
            command.Parameters.AddWithValue("@jobId", jobIdBytes);
            command.Parameters.AddWithValue("@processId", job.ProcessId.Value);
            command.Parameters.AddWithValue("@progress", job.Progress.Value);
            command.Parameters.AddWithValue("@startTime", job.StartTime.ToFileTimeUtc());
            command.Parameters.AddWithValue("@data", data);
            command.ExecuteNonQuery();

            transaction.Commit();
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
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        _logger.DeletingJob(job);

        try
        {
            var jobIdBytes = job.Value.ToByteArray();

            using var connection = CreateConnection();
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM Jobs WHERE JobId = @jobId";
            command.Parameters.AddWithValue("@jobId", jobIdBytes);
            command.ExecuteNonQuery();
            transaction.Commit();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while ending job {JobId}", job.Value);
        }

        UpdateJobTimestamp();
    }

    /// <inheritdoc />
    public void UpdateProgress(JobId jobId, Percent value)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        _logger.UpdatingJobProgress(jobId, value);

        try
        {
            var jobIdBytes = jobId.Value.ToByteArray();

            using var connection = CreateConnection();
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Jobs SET Progress = @progress WHERE JobId = @jobId";
            command.Parameters.AddWithValue("@progress", value.Value);
            command.Parameters.AddWithValue("@jobId", jobIdBytes);
            command.ExecuteNonQuery();
            transaction.Commit();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while updating job {JobId}", jobId);
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
            _globalConnection?.Close();
            _shutdownToken.Cancel();
            _subject.Dispose();
            _syncArray.Dispose();
            _jobs.Dispose();
        }

        _isDisposed = true;
    }
}
