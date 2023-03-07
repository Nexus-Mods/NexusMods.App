using Cloudtoid.Interprocess;
using Microsoft.Extensions.Logging;

namespace NexusMods.DataModel.Interprocess;

/// <summary>
/// Implementation of <see cref="IMessageProducer{T}"/> that uses an interprocess queue.
/// </summary>
public sealed class InterprocessProducer<T> : IMessageProducer<T>, IDisposable where T : IMessage
{
    private readonly ILogger<InterprocessProducer<T>> _logger;
    private readonly IPublisher _queue;
    private readonly string _queueName;

    /// <summary>
    /// Creates a producer capable of sending messages across process boundaries.
    /// </summary>
    /// <param name="logger">Logs received interprocess messages.</param>
    /// <param name="queueFactory">Factory used to create queues for receiving inter-process messages.</param>
    /// <remarks>
    ///    This method is usually called from DI container, not by user directly.
    /// </remarks>
    public InterprocessProducer(ILogger<InterprocessProducer<T>> logger, IQueueFactory queueFactory)
    {
        _logger = logger;
        _queueName = "NexusMods.DataModel.Interprocess." + typeof(T).Name;

        _queue = queueFactory.CreatePublisher(new QueueOptions(
            _queueName,
            T.MaxSize * 16));
    }

    /// <inheritdoc />
    public void Dispose() => _queue.Dispose();

    /// <summary>
    /// Writes a message to the queue.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="token"></param>
    /// <exception cref="TaskCanceledException"></exception>
    public async ValueTask Write(T message, CancellationToken token)
    {
        _logger.LogTrace("Writing message {Message} to queue {Queue}", message, _queueName);

        while (!token.IsCancellationRequested)
        {
            if (WriteInner(message))
                return;

            _logger.LogDebug("Failed to write {Message} to queue {Queue}, retrying", message, _queueName);
            await Task.Delay(100, token);
        }

        throw new TaskCanceledException();
    }

    /// <summary>
    /// Split out so we can use stack allocation in an otherwise async method.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private bool WriteInner(T message)
    {
        // This means we re-encode the message on every attempt, but it is assumed that failed messages
        // are rare and the cost of re-encoding is negligible.
        Span<byte> buffer = stackalloc byte[T.MaxSize];
        var used = message.Write(buffer);
        return _queue.TryEnqueue(buffer[..used]);
    }
}
