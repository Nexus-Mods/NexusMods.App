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
public sealed class SqliteIPC : IDisposable, IInterprocessJobManager
{
    // NOTE(erri120): Microsoft.Data.Sqlite has a hardcoded timeout of 150ms.
    // This timeout is used when the database is busy.
    private static readonly TimeSpan SqliteDefaultTimeout = TimeSpan.FromMilliseconds(150);

    private static readonly TimeSpan RetentionTime = TimeSpan.FromSeconds(10); // Keep messages for 10 seconds
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(10); // Cleanup every 10 minutes
    private const int CleanupJitter = 2000; // Jitter cleanup by up to 2 second

    // The Reader and Writer loops are the only places that read/write to the database.
    // As such, they are the only places that can timeout due to the database being busy.
    // To prevent this, we can set the interval for both loops, so that they are out of sync and hopefully
    // are not active when the other is doing something.
    // This is ofc limited to the current process only.
    internal static readonly TimeSpan ReaderLoopInterval = SqliteDefaultTimeout + TimeSpan.FromMilliseconds(50);
    internal static readonly TimeSpan WriterLoopInterval = SqliteDefaultTimeout;

    private static readonly ProcessId OwnProcessId = ProcessId.From((uint)Environment.ProcessId);

    private bool _isDisposed;

    private readonly ILogger<SqliteIPC> _logger;
    private readonly CancellationTokenSource _shutdownToken;
    private readonly JsonSerializerOptions _jsonSettings;

    private readonly string _connectionString;
    private readonly ISharedArray _syncArray;
    private readonly SqliteConnection? _globalConnection; // only for in-memory connections

    private readonly Subject<(string Queue, byte[] Message)> _subject = new();
    private readonly SourceCache<IInterprocessJob, JobId> _jobs = new(x => x.JobId);

    // This timeout should be higher than the maximum time it takes for the writer loop to complete.
    // Taking the Sqlite timeout into account, setting this to 1 second should be enough.
    private static readonly TimeSpan SemaphoreMaxTimeout = TimeSpan.FromSeconds(1);

    // SemaphoreSlim is used for lightweight synchronization across threads.
    private readonly SemaphoreSlim _insertSemaphore = new(1, 1);
    private readonly SemaphoreSlim _updateSemaphore = new(1, 1);
    private readonly SemaphoreSlim _deleteSemaphore = new(1, 1);
    private readonly SemaphoreSlim _messageSemaphore = new(1, 1);

    // Queues for the WriterLoop.
    private readonly Queue<(string queue, byte[] bytes, long timestamp)> _messagesToSend = new(32);
    private readonly Queue<IInterprocessJob> _jobsToInsert = new(16);
    private readonly Queue<(JobId jobId, Percent progress)> _jobsToUpdate = new(64);
    private readonly Queue<JobId> _jobsToDelete = new(32);

    // Written to by the WriterLoop and read by the ReaderLoop.
    // This is used to prevent the ReaderLoop from broadcasting the same message twice.
    private readonly List<long> WrittenMessageIds = new(16);

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

        // 0 = last message id
        // 1 = last job timestamp
        const int syncSlots = 2;

        var builder = new SqliteConnectionStringBuilder
        {
            // Microsoft.Data.Sqlite has in-built pooling.
            Pooling = true,
            // The IPC doesn't use foreign keys.
            ForeignKeys = false
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

        EnsureTables(settings.UseInMemoryDataModel);

        var startId = GetStartId();
        Task.Run(() => ReaderLoop(startId, _shutdownToken.Token));
        Task.Run(() => WriterLoop(_shutdownToken.Token));
        Task.Run(() => CleanupLoop(_shutdownToken.Token));
    }

    private SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        return connection;
    }

    private void EnsureTables(bool isInMemory)
    {
        using var connection = CreateConnection();

        // in-memory connections use a shared-cache instead of the WAL
        if (!isInMemory)
        {
            // Changing the journal mode to write-ahead log can't be done within a transaction.
            using var pragmaCommand = connection.CreateCommand();
            pragmaCommand.CommandText = "PRAGMA journal_mode = WAL";
            pragmaCommand.ExecuteNonQuery();
        }

        using var ipcTableCommand = connection.CreateCommand();
        ipcTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS Ipc (Id INTEGER PRIMARY KEY ASC AUTOINCREMENT, Queue TEXT, Data BLOB, TimeStamp INTEGER)";
        ipcTableCommand.ExecuteNonQuery();

        using var jobsTableCommand = connection.CreateCommand();
        jobsTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS Jobs (JobId BLOB PRIMARY KEY, ProcessId INTEGER, Progress REAL, StartTime INTEGER, Data BLOB)";
        jobsTableCommand.ExecuteNonQuery();
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

    private async Task WriterLoop(CancellationToken cancellationToken)
    {
        // The writer loop takes all outstanding changes and writes them to Sqlite.
        // These changes have already been broadcasted to subscribers in the CURRENT
        // process, but we use a Sqlite DB for inter-process communications (IPC).

        var sw = new Stopwatch();
        ulong lastId = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            sw.Restart();

            try
            {
                WriteJobsToDatabase(cancellationToken);
                lastId = WriteMessagesToDatabase(lastId, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception while writing to the database");
            }

            var elapsed = sw.Elapsed;
            if (elapsed > SqliteDefaultTimeout)
            {
                _logger.LogDebug("WriterLoop was locked by Sqlite for {}ms", elapsed.TotalMilliseconds);
                continue;
            }

            await Task.Delay(WriterLoopInterval - elapsed, cancellationToken);
        }
    }

    private ulong WriteMessagesToDatabase(ulong lastId, CancellationToken cancellationToken)
    {
        using var waiter = _messageSemaphore.CustomWait(SemaphoreMaxTimeout, cancellationToken);
        if (!waiter.HasEntered)
        {
            _logger.LogDebug("Failed to enter the message semaphore in the WriterLoop within {}ms", SemaphoreMaxTimeout.TotalMilliseconds);
            return lastId;
        }

        // Nothing to do
        if (_messagesToSend.Count == 0) return lastId;

        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();

        command.CommandText = "INSERT INTO Ipc (Queue, Data, TimeStamp) VALUES (@queue, @data, @timestamp); SELECT last_insert_rowid();";

        var queueParameter = command.CreateParameter();
        queueParameter.ParameterName = "@queue";
        command.Parameters.Add(queueParameter);

        var dataParameter = command.CreateParameter();
        dataParameter.ParameterName = "@data";
        command.Parameters.Add(dataParameter);

        var timestampParameter = command.CreateParameter();
        timestampParameter.ParameterName = "@timestamp";
        command.Parameters.Add(timestampParameter);

        var newLastId = lastId;
        while (_messagesToSend.TryDequeue(out var tuple))
        {
            var (queue, bytes, timestamp) = tuple;
            queueParameter.Value = queue;
            dataParameter.Value = bytes;
            timestampParameter.Value = timestamp;

            var result = command.ExecuteScalar();
            newLastId = result == DBNull.Value ? newLastId : Convert.ToUInt64(result);
            WrittenMessageIds.Add((long)newLastId);
        }

        transaction.Commit();

        UpdateLastMessageId(newLastId);
        return newLastId;
    }

    private void WriteJobsToDatabase(CancellationToken cancellationToken)
    {
        using var insertWaiter = _insertSemaphore.CustomWait(SemaphoreMaxTimeout, cancellationToken);
        using var updateWaiter = _updateSemaphore.CustomWait(SemaphoreMaxTimeout, cancellationToken);
        using var deleteWaiter = _deleteSemaphore.CustomWait(SemaphoreMaxTimeout, cancellationToken);
        if (!insertWaiter.HasEntered || !updateWaiter.HasEntered || !deleteWaiter.HasEntered)
        {
            _logger.LogDebug("Failed to enter one or more semaphores!");
            return;
        }

        // Nothing to do
        if (_jobsToInsert.Count == 0 && _jobsToUpdate.Count == 0 && _jobsToDelete.Count == 0) return;

        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        if (_jobsToInsert.Count > 0) InsertJobs(connection, _jobsToInsert);
        if (_jobsToUpdate.Count > 0) UpdateJobs(connection, _jobsToUpdate);
        if (_jobsToDelete.Count > 0) DeleteJobs(connection, _jobsToDelete);

        transaction.Commit();

        UpdateJobTimestamp();
    }

    private void InsertJobs(SqliteConnection connection, Queue<IInterprocessJob> jobsToCreate)
    {
        using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = "INSERT INTO Jobs (JobId, ProcessId, Progress, StartTime, Data) VALUES (@jobId, @processId, @progress, @startTime, @data);";

        var jobIdParameter = insertCommand.CreateParameter();
        jobIdParameter.ParameterName = "@jobId";
        insertCommand.Parameters.Add(jobIdParameter);

        var processIdParameter = insertCommand.CreateParameter();
        processIdParameter.ParameterName = "@processId";
        insertCommand.Parameters.Add(processIdParameter);

        var progressParameter = insertCommand.CreateParameter();
        progressParameter.ParameterName = "@progress";
        insertCommand.Parameters.Add(progressParameter);

        var startTimeParameters = insertCommand.CreateParameter();
        startTimeParameters.ParameterName = "@startTime";
        insertCommand.Parameters.Add(startTimeParameters);

        var dataParameter = insertCommand.CreateParameter();
        dataParameter.ParameterName = "@data";
        insertCommand.Parameters.Add(dataParameter);

        // re-using parameter definitions for performance
        while (jobsToCreate.TryDequeue(out var job))
        {
            jobIdParameter.Value = job.JobId.Value.ToByteArray();
            processIdParameter.Value = job.ProcessId.Value;
            progressParameter.Value = job.Progress.Value;
            startTimeParameters.Value = job.StartTime.ToFileTimeUtc();
            dataParameter.Value = JsonSerializer.SerializeToUtf8Bytes(job.Payload, _jsonSettings);
        }
    }

    private static void UpdateJobs(SqliteConnection connection, Queue<(JobId, Percent)> jobsToUpdate)
    {
        using var updateCommand = connection.CreateCommand();
        updateCommand.CommandText = "UPDATE Jobs SET Progress = @progress WHERE JobId = @jobId";

        var jobIdParameter = updateCommand.CreateParameter();
        jobIdParameter.ParameterName = "@jobId";
        updateCommand.Parameters.Add(jobIdParameter);

        var progressParameter = updateCommand.CreateParameter();
        progressParameter.ParameterName = "@progress";
        updateCommand.Parameters.Add(progressParameter);

        // re-using parameter definitions for performance
        while (jobsToUpdate.TryDequeue(out var tuple))
        {
            var (jobId, percent) = tuple;
            jobIdParameter.Value = jobId.Value.ToByteArray();
            progressParameter.Value = percent.Value;

            updateCommand.ExecuteNonQuery();
        }
    }

    private static void DeleteJobs(SqliteConnection connection, Queue<JobId> jobsToDelete)
    {
        using var deleteCommand = connection.CreateCommand();
        deleteCommand.CommandText = "DELETE FROM Jobs WHERE JobId = @jobId";

        var jobIdParameter = deleteCommand.CreateParameter();
        jobIdParameter.ParameterName = "@jobId";
        deleteCommand.Parameters.Add(jobIdParameter);

        // re-using parameter definitions for performance
        while (jobsToDelete.TryDequeue(out var jobId))
        {
            jobIdParameter.Value = jobId.Value.ToByteArray();
            deleteCommand.ExecuteNonQuery();
        }
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

    private readonly Queue<(string, byte[])> _updates = new(256);
    private long ProcessMessages(long lastId)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var writtenIds = WrittenMessageIds.ToArray();
        WrittenMessageIds.Clear();

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

                // Don't broadcast the same id twice.
                if (Array.BinarySearch(writtenIds, lastId) >= 0) continue;

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

            // NOTE (erri120): We're only reading jobs from other processes. Jobs changes within the current process
            // have already been broadcasted.

            command.CommandText = "SELECT JobId, ProcessId, Progress, StartTime, Data FROM Jobs WHERE ProcessId != @processId";
            command.Parameters.AddWithValue("@processId", OwnProcessId.Value);

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

        // early exist if nothing changed
        if (_updatedJobs.Count == 0 && _newJobs.Count == 0) return;

        _jobs.Edit(editable =>
        {
            var seen = new HashSet<JobId>();

            while (_updatedJobs.TryDequeue(out var tuple))
            {
                var (jobId, progress) = tuple;
                seen.Add(jobId);

                var item = editable.Lookup(jobId);
                if (!item.HasValue || item.Value.Progress >= progress) continue;

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
        });

        _logger.DoneProcessing();
    }

    /// <summary>
    /// Send a message to the queue.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="message"></param>
    public void Send(string queue, ReadOnlySpan<byte> message)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var bytes = message.ToArray();
        _subject.OnNext((queue, bytes));

        using var waiter = _messageSemaphore.CustomWait(SemaphoreMaxTimeout);
        if (waiter.HasEntered)
        {
            _messagesToSend.Enqueue((queue, bytes, DateTime.UtcNow.ToFileTimeUtc()));
        }
        else
        {
            _logger.LogDebug("Failed to enter the message semaphore within {}ms", SemaphoreMaxTimeout.TotalMilliseconds);
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

        _jobs.Edit(updater =>
        {
            var newJob = new InterprocessJob(job.JobId, this, job.ProcessId, job.StartTime, job.Progress, job.Payload);
            updater.AddOrUpdate(newJob);
        });

        using var waiter = _insertSemaphore.CustomWait(SemaphoreMaxTimeout);
        if (waiter.HasEntered)
        {
            _jobsToInsert.Enqueue(job);
        }
        else
        {
            _logger.LogDebug("Failed to enter the insert semaphore within {}ms", SemaphoreMaxTimeout.TotalMilliseconds);
        }
    }

    /// <inheritdoc />
    public void UpdateProgress(JobId jobId, Percent value)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        _logger.UpdatingJobProgress(jobId, value);

        _jobs.Edit(updater =>
        {
            var optional = updater.Lookup(jobId);
            if (!optional.HasValue) return;

            var existing = optional.Value;
            if (existing.Progress >= value) return;

            existing.Progress = value;
            updater.AddOrUpdate(existing);
        });

        using var waiter = _updateSemaphore.CustomWait(SemaphoreMaxTimeout);
        if (waiter.HasEntered)
        {
            _jobsToUpdate.Enqueue((jobId, value));
        }
        else
        {
            _logger.LogDebug("Failed to enter the update semaphore within {}ms", SemaphoreMaxTimeout.TotalMilliseconds);
        }
    }

    /// <inheritdoc />
    public void EndJob(JobId job)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        _logger.DeletingJob(job);

        _jobs.Edit(updater => updater.Remove(job));

        using var waiter = _deleteSemaphore.CustomWait(SemaphoreMaxTimeout);
        if (waiter.HasEntered)
        {
            _jobsToDelete.Enqueue(job);
        }
        else
        {
            _logger.LogDebug("Failed to enter the delete semaphore within {}ms", SemaphoreMaxTimeout.TotalMilliseconds);
        }
    }

    private void UpdateLastMessageId(ulong lastId)
    {
        var prevId = _syncArray.Get(0);
        while (true)
        {
            if (prevId >= lastId) break;
            if (_syncArray.CompareAndSwap(0, prevId, lastId)) break;
            prevId = _syncArray.Get(0);
        }
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

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        try
        {
            _shutdownToken.Cancel();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while requesting cancellation");
        }

        _subject.Dispose();
        _jobs.Dispose();
        _syncArray.Dispose();
        _globalConnection?.Close();

        _insertSemaphore.Dispose();
        _updateSemaphore.Dispose();
        _deleteSemaphore.Dispose();
    }
}
