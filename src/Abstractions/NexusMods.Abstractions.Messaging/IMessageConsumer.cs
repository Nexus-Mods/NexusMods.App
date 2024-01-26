namespace NexusMods.Abstractions.Messaging;

/// <summary>
/// A message consumer for receiving messages from other processes.
/// </summary>
/// <typeparam name="T">The types of messages being sent/received on the queue </typeparam>
public interface IMessageConsumer<out T>
{
    /// <summary>
    /// An IObservable of messages of the given type
    /// </summary>
    public IObservable<T> Messages { get; }
}
