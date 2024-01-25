using System.Reactive.Subjects;
using NexusMods.Abstractions.Messaging;

namespace NexusMods.DataModel.Messaging;

internal class MessageProducer<T>(MessageBus messageBus) : IMessageProducer<T>
{
    private readonly Subject<T> _subject = messageBus.GetSubject<T>();

    public async ValueTask Write(T message, CancellationToken token)
    {
        // In the future Rx.NET will support for async sends, but for now we'll just use a new task.
        await Task.Run(() => _subject.OnNext(message), token);
    }
}
