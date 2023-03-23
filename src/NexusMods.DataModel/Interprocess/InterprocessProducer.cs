namespace NexusMods.DataModel.Interprocess;

/// <summary>
/// Implementation of <see cref="IMessageProducer{T}"/> that uses an interprocess queue.
/// </summary>
public sealed class InterprocessProducer<T> : IMessageProducer<T>, IDisposable where T : IMessage
{
    private readonly string _queueName;
    private readonly SqliteIPC _sqliteIpc;

    /// <summary>
    /// Creates a producer capable of sending messages across process boundaries.
    /// </summary>
    /// <param name="sqliteIpc">Provides access to SQLite based IPC implementation.</param>
    /// <remarks>
    ///    This method is usually called from DI container, not by user directly.
    /// </remarks>
    public InterprocessProducer(SqliteIPC sqliteIpc)
    {
        _queueName = typeof(T).Name;
        _sqliteIpc = sqliteIpc;
    }

    /// <inheritdoc />
    public void Dispose() => _sqliteIpc.Dispose();

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
    /// <param name="message">The message to write.</param>
    /// <returns></returns>
    private void WriteInner(T message)
    {
        // TODO: SliceFast here. https://github.com/Nexus-Mods/NexusMods.App/issues/214
        Span<byte> buffer = stackalloc byte[T.MaxSize];
        var used = message.Write(buffer);
        _sqliteIpc.Send(_queueName, buffer[..used]);
    }
}
