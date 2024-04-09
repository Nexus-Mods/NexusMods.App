using System.Reactive.Linq;
using System.Reactive.Subjects;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Settings;

internal class SettingsManager : ISettingsManager
{
    private readonly Subject<(Type, object)> _subject = new();
    private readonly Dictionary<Type, object> _values = new();

    public void Set<T>(T value) where T : class, ISettings, new()
    {
        _values[typeof(T)] = value;
        _subject.OnNext((typeof(T), value));
    }

    public T Get<T>() where T : class, ISettings, new()
    {
        if (_values.TryGetValue(typeof(T), out var obj)) return (obj as T)!;

        var value = new T();
        Set(value);

        return value;
    }

    public IObservable<T> GetChanges<T>() where T : class, ISettings, new()
    {
        return _subject
            .Where(tuple => tuple.Item1 == typeof(T))
            .Select(tuple => (tuple.Item2 as T)!);
    }
}
