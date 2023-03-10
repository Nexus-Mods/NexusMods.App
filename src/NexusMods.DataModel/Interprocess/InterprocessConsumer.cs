using System.Reactive.Linq;

namespace NexusMods.DataModel.Interprocess;

/// <summary>
/// Implementation of <see cref="IMessageConsumer{T}"/> that uses an interprocess queue.
/// </summary>
public class InterprocessConsumer<T> : IMessageConsumer<T> where T : IMessage
{
    private readonly string _queueName;
    private readonly SqliteIPC _sqliteIpc;

    /// <summary>
    /// Creates a consumer capable of receiving messages across process boundaries.
    /// </summary>
    /// <param name="sqliteIpc">Provides access to SQLite based IPC implementation.</param>
    /// <remarks>
    ///    This method is usually called from DI container, not by user directly.
    /// </remarks>
    public InterprocessConsumer(SqliteIPC sqliteIpc)
    {
        _queueName = typeof(T).Name;
        _sqliteIpc = sqliteIpc;
    }

    public IObservable<T> Messages => _sqliteIpc.Messages
        .Where(msg => msg.Queue == _queueName)
        .Select(msg => (T)T.Read(msg.Message));

}
