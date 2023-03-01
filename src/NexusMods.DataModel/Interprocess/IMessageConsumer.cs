namespace NexusMods.DataModel.Interprocess;

/// <summary>
/// A message consumer for receiving messages from other processes.
/// </summary>
/// <typeparam name="T">The types of messages being sent/received on the queue </typeparam>
public interface IMessageConsumer<T> where T : IMessage
{
    /// <summary>
    /// A subject that will receive messages from the queue.
    /// </summary>
    public IObservable<T> Messages { get; }
}