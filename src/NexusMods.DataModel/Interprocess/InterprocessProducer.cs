using Microsoft.Extensions.Logging;

namespace NexusMods.DataModel.Interprocess;

/// <summary>
/// Implementation of <see cref="IMessageProducer{T}"/> that uses an interprocess queue.
/// </summary>
/// <typeparam name="T"></typeparam>
public class InterprocessProducer<T> : IMessageProducer<T> where T : IMessage
{
    private readonly string _queueName;
    private readonly SqliteIPC _sqliteIpc;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="queueFactory">Queue Factory</param>
    public InterprocessProducer(ILogger<InterprocessProducer<T>> logger, SqliteIPC sqliteIpc)
    {
        _queueName = typeof(T).Name;
        _sqliteIpc = sqliteIpc;
    }

    /// <summary>
    /// Writes a message to the queue.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="token"></param>
    /// <exception cref="TaskCanceledException"></exception>
    public ValueTask Write(T message, CancellationToken token)
    {
        WriteInner(message);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Split out so we can use stack allocation in an otherwise async method.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private void WriteInner(T message)
    {
        // This means we re-encode the message on every attempt, but it is assumed that failed messages
        // are rare and the cost of re-encoding is negligible.
        Span<byte> buffer = stackalloc byte[T.MaxSize];
        var used = message.Write(buffer);
        _sqliteIpc.Send(_queueName, buffer[..used]);
    }
}
