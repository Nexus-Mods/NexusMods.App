using NexusMods.Abstractions.Messaging;

namespace NexusMods.DataModel.Messaging;


/// <summary>
/// A message consumer for receiving messages from producers in this process
/// </summary>
/// <typeparam name="T"></typeparam>
internal class MessageConsumer<T> : IMessageConsumer<T>
{
    /// <summary>
    /// DI constructor.
    /// </summary>
    /// <param name="messageBus"></param>
    public MessageConsumer(MessageBus messageBus)
    {
        Messages = messageBus.GetSubject<T>();
    }

    /// <inheritdoc />
    public IObservable<T> Messages { get; }
}
