namespace NexusMods.Abstractions.Messaging;

/// <summary>
/// A message producer for sending messages to other processes.
/// </summary>
/// <typeparam name="T">Message type to send, each message type gets its own queue</typeparam>
public interface IMessageProducer<in T>
{
    /// <summary>
    /// Sends a message to the queue.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="token">Can be used to cancel this operation.</param>
    public ValueTask Write(T message, CancellationToken token);
}
