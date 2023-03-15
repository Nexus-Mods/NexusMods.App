using System.Data.SQLite;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
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
public class SqliteIPC : IDisposable
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

    public IObservable<(string Queue, byte[] Message)> Messages => _subject;

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
    }

    private async Task ReaderLoop(long lastId, CancellationToken shutdownTokenToken)
    {
        while (!shutdownTokenToken.IsCancellationRequested)
        {
            lastId = ProcessMessages(lastId);

            // TODO: Bug, FileInfo is a cached field.
            var lastEdit = _storePath.FileInfo;

            var elapsed = DateTime.UtcNow;
            while (!shutdownTokenToken.IsCancellationRequested)
            {
                var currentEdit = _storePath.FileInfo;
                if (currentEdit.Size != lastEdit.Size || currentEdit.LastWriteTimeUtc != lastEdit.LastWriteTimeUtc)
                {
                    break;
                }
                await Task.Delay(ShortPollInterval, shutdownTokenToken);

                if (DateTime.UtcNow - elapsed > LongPollInterval)
                {
                    break;
                }
            }
        }
    }

    private long GetStartId()
    {
        using var cmd = new SQLiteCommand(
            "SELECT MAX(Id) FROM Ipc WHERE TimeStamp >= @current", _conn);
        // Subtract 1 second to ensure we don't miss any messages that were written in the last second.
        cmd.Parameters.AddWithValue("@current", DateTime.UtcNow.ToFileTimeUtc());
        var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            return reader.IsDBNull(0) ? 0L : reader.GetInt64(0);
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
            _logger.LogTrace("Sending {Bytes} byte message to queue {Queue}", Size.From(message.Length), queue);
            using var cmd = new SQLiteCommand(
                "INSERT INTO Ipc (Queue, Data, TimeStamp) VALUES (@queue, @data, @timestamp);",
                _conn);
            cmd.Parameters.AddWithValue("@queue", queue);
            cmd.Parameters.AddWithValue("@data", message.ToArray());
            cmd.Parameters.AddWithValue("@timestamp",
                DateTime.UtcNow.ToFileTimeUtc());
            cmd.ExecuteNonQuery();
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
    }
}
