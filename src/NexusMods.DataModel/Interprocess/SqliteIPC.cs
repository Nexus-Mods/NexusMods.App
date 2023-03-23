using System.Data.SQLite;
using System.Diagnostics;
using System.Reactive.Subjects;
using DynamicData;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Interprocess.Jobs;
using NexusMods.DataModel.Interprocess.Messages;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Paths;

namespace NexusMods.DataModel.Interprocess;

/// <summary>
/// Fairly simple interprocess communication system using SQLite. This is not intended to be a high performance system,
/// but rather a simple way to communicate between processes. Messages are stored in a SQLite database and read by a
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

    private readonly AbsolutePath _storePath;
    private readonly SQLiteConnection _conn;

    private readonly Subject<(string Queue, byte[] Message)> _subject = new();
    private readonly CancellationTokenSource _shutdownToken;
    private readonly ILogger<SqliteIPC> _logger;
    private readonly AbsolutePath _syncPath;
    private readonly SharedArray _syncArray;

    private SourceCache<IInterprocessJob, JobId> _jobs = new(x => x.JobId);

    /// <summary>
    /// Allows you to subscribe to newly incoming IPC messages.
    /// </summary>
    public IObservable<(string Queue, byte[] Message)> Messages => _subject;
    public IObservable<IChangeSet<IInterprocessJob, JobId>> Jobs => _jobs.Connect();

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="logger">Allows for logging of messages.</param>
    /// <param name="storePath">Physical location on disk where the IPC database is stored.</param>
    public SqliteIPC(ILogger<SqliteIPC> logger, AbsolutePath storePath)
    {
        _logger = logger;
        _storePath = storePath;

        var connectionString = string.Intern($"Data Source={_storePath}");
        _conn = new SQLiteConnection(connectionString);
        _conn.Open();
        _syncPath = storePath.AppendExtension(new Extension(".sync"));
        _syncArray = new SharedArray(_syncPath, 2);

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
        var oldTime = DateTime.UtcNow - RetentionTime;

        _logger.LogTrace("Cleaning up old IPC messages");
        await using var cmd = new SQLiteCommand(
            "DELETE from Ipc WHERE TimeStamp < @timestamp",
            _conn);
        cmd.Parameters.AddWithValue("@timestamp", oldTime.ToFileTimeUtc());
        await cmd.ExecuteNonQueryAsync(token);

        foreach (var job in _jobs.Items)
        {
            try
            {
                Process.GetProcessById((int)job.ProcessId.Value);
            }
            catch (ArgumentException _)
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
        using var cmd = new SQLiteCommand(
            "SELECT MAX(Id) FROM Ipc", _conn);
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
        try
        {
            using var cmd = new SQLiteCommand(
                "SELECT Id, Queue, Data FROM Ipc WHERE Id > @lastId", _conn);
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
        using var cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Ipc (Id INTEGER PRIMARY KEY AUTOINCREMENT, Queue VARCHAR, Data BLOB, TimeStamp INTEGER)", _conn);
        cmd.ExecuteNonQuery();

        using var cmd2 = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Jobs " +
                                           "(JobId BLOB PRIMARY KEY, " +
                                           "ProcessId INTEGER, " +
                                           "Progress REAL, " +
                                           "Description TEXT," +
                                           "JobType TEXT," +
                                           "StartTime INTEGER," +
                                           "Data BLOB)", _conn);
        cmd2.ExecuteNonQuery();
    }



    private void ProcessJobs()
    {
        try
        {
            _logger.LogTrace("Processing jobs");
            using var cmd = new SQLiteCommand(
                "SELECT JobId, ProcessId, Progress, Description, JobType, StartTime, Data FROM Jobs",
                _conn);
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
                        _logger.LogTrace("Job {JobId} progress is {Progress}", jobId, progress);
                        continue;
                    }

                    _logger.LogTrace("Found new job {JobId}", jobId);
                    var processId = ProcessId.From((uint)reader.GetInt64(1));
                    var description = reader.GetString(3);
                    var jobType = Enum.Parse<JobType>(reader.GetString(4));
                    var startTime =
                        DateTime.FromFileTimeUtc(reader.GetInt64(5));
                    var size = reader.GetBytes(6, 0, null, 0, 0);
                    var bytes = new byte[size];
                    reader.GetBytes(6, 0, bytes, 0, bytes.Length);

                    var newJob = new InterprocessJob(jobId, this, jobType,
                        processId, description, bytes, startTime, progress);
                    editable.AddOrUpdate(newJob);
                }

                foreach (var key in editable.Keys)
                {
                    if (seen.Contains(key))
                        continue;

                    _logger.LogTrace("Removing job {JobId}", key);
                    editable.Remove(key);
                }
                _logger.LogTrace("Done Processing");
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
        try
        {
            _logger.LogTrace("Sending {Bytes} byte message to queue {Queue}", Size.FromLong(message.Length), queue);
            using var cmd = new SQLiteCommand(
                "INSERT INTO Ipc (Queue, Data, TimeStamp) VALUES (@queue, @data, @timestamp);",
                _conn);
            cmd.Parameters.AddWithValue("@queue", queue);
            cmd.Parameters.AddWithValue("@data", message.ToArray());
            cmd.Parameters.AddWithValue("@timestamp",
                DateTime.UtcNow.ToFileTimeUtc());
            cmd.ExecuteNonQuery();
            var lastId = (ulong)cmd.Connection.LastInsertRowId;
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
    /// Dispose of the IPC connection.
    /// </summary>
    public void Dispose()
    {
        _shutdownToken.Cancel();
        _conn.Dispose();
        _subject.Dispose();
        _syncArray.Dispose();
    }

    public void CreateJob(IInterprocessJob job)
    {
        try
        {
            _logger.LogInformation("Creating job {JobId} of type {JobType}", job.JobId, job.GetType().Name);
            using var cmd = new SQLiteCommand(
                "INSERT INTO Jobs (JobId, ProcessId, Progress, Description, JobType, StartTime, Data) " +
                "VALUES (@jobId, @processId, @progress, @description, @jobType, @startTime, @data);", _conn);
            cmd.Parameters.AddWithValue("@jobId", job.JobId.Value.ToByteArray());
            cmd.Parameters.AddWithValue("@processId", job.ProcessId.Value);
            cmd.Parameters.AddWithValue("@progress", job.Progress.Value);
            cmd.Parameters.AddWithValue("@description", job.Description);
            cmd.Parameters.AddWithValue("@jobType", job.JobType.ToString());
            cmd.Parameters.AddWithValue("@startTime", job.StartTime.ToFileTimeUtc());
            cmd.Parameters.AddWithValue("@data", job.Data);
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

    public void EndJob(JobId job)
    {
        _logger.LogInformation("Deleting job {JobId}", job);
        {
            using var cmd = new SQLiteCommand(
                "DELETE FROM Jobs WHERE JobId = @jobId", _conn);
            cmd.Parameters.AddWithValue("@jobId", job.Value);
            cmd.ExecuteNonQuery();
        }
        UpdateJobTimestamp();

    }

    public void UpdateProgress(JobId jobId, Percent value)
    {
        _logger.LogTrace("Updating job {JobId} progress to {Percent}", jobId, value);
        {
            using var cmd = new SQLiteCommand(
                "UPDATE Jobs SET Progress = @progress WHERE JobId = @jobId", _conn);
            cmd.Parameters.AddWithValue("@progress", value.Value);
            cmd.Parameters.AddWithValue("@jobId", jobId.Value);
            cmd.ExecuteNonQuery();
        }
        UpdateJobTimestamp();
    }
}
