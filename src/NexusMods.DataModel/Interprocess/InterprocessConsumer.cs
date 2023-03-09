using System.Reactive.Linq;

namespace NexusMods.DataModel.Interprocess;

public class InterprocessConsumer<T> : IMessageConsumer<T> where T : IMessage
{
    private readonly string _queueName;
    private readonly SqliteIPC _sqliteIpc;

    public InterprocessConsumer(SqliteIPC sqliteIpc)
    {
        _queueName = typeof(T).Name;
        _sqliteIpc = sqliteIpc;
    }

    public IObservable<T> Messages => _sqliteIpc.Messages
        .Where(msg => msg.Queue == _queueName)
        .Select(msg => (T)T.Read(msg.Message));

}
