using System.Collections.Concurrent;
using System.Reactive.Subjects;

namespace NexusMods.DataModel.Messaging;

/// <summary>
/// The default in-memory message bus.
/// </summary>
internal class MessageBus
{
    /// <summary>
    /// A dictionary of subjects, keyed by the message type. Stored as an object due to the generic conversion
    /// problem. The producer/consumer will cast it to the correct type during creation, so it's not a big deal.
    /// </summary>
    private readonly ConcurrentDictionary<Type, object> _subjects = new();

    /// <summary>
    /// Gets a subject for the specified message type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public Subject<T> GetSubject<T>()
    {
        var type = typeof(T);
        if (_subjects.TryGetValue(type, out var subject))
            return (Subject<T>)subject;

        subject = new Subject<T>();
        if (!_subjects.TryAdd(type, subject))
        {
            subject = _subjects[type];
        }

        return (Subject<T>)subject;
    }

}
