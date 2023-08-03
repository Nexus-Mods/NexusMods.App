namespace NexusMods.DataModel.Interprocess;

/// <summary>
/// A message producer for sending messages to other processes.
/// </summary>
/// <typeparam name="T">Message type to send, each message type gets its own queue</typeparam>
public interface IMessageProducer<in T> where T : IMessage
{
    /// <summary>
    /// Sends a message to the queue.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="token">Can be used to cancel this operation.</param>
    public ValueTask Write(T message, CancellationToken token);

    /// <summary>
    /// Ensures all messages in-flight have been saved to the database.
    /// This is done using an <see cref="EventWaitHandle"/> and this call
    /// will block the entire thread.
    /// </summary>
    public void EnsureWrite(CancellationToken cancellationToken);
}
