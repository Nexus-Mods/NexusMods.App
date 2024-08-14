using DynamicData;
using JetBrains.Annotations;
using ObservableCollections;
using R3;

namespace NexusMods.App.UI.Extensions;

[PublicAPI]
public static class R3Extensions
{
    public static (ObservableHashSet<TValue> set, IDisposable disposable) ToObservableHashSet<TValue>(this IObservable<IChangeSet<TValue>> source)
        where TValue : notnull
    {
        var set = new ObservableHashSet<TValue>();

        var disposable = source.ToObservable().Subscribe(set, static (changes, set) => set.ApplyChanges(changes));
        return (set, disposable);
    }

    public static (ObservableList<TValue> list, IDisposable disposable) ToObservableList<TValue>(this IObservable<IChangeSet<TValue>> source)
        where TValue : notnull
    {
        var list = new ObservableList<TValue>();

        var disposable = source.ToObservable().Subscribe(list, static (changes, list) => list.Clone(changes));
        return (list, disposable);
    }

    public static (ObservableDictionary<TKey, TValue> dict, IDisposable disposable) ToObservableDictionary<TKey, TValue>(this IObservable<IChangeSet<TValue, TKey>> source)
        where TKey : notnull
        where TValue : notnull
    {
        var dict = new ObservableDictionary<TKey, TValue>();

        var disposable = source.ToObservable().Subscribe(dict, static (changes, dict) => dict.ApplyChanges(changes));
        return (dict, disposable);
    }

    public static void ApplyChanges<TValue>(this ObservableHashSet<TValue> set, IChangeSet<TValue> changes)
        where TValue : notnull
    {
        foreach (var change in changes)
        {
            switch (change.Reason)
            {
                case ListChangeReason.Add:
                    set.Add(change.Item.Current);
                    break;
                case ListChangeReason.AddRange:
                    set.AddRange(change.Range);
                    break;
                case ListChangeReason.Replace:
                    set.Remove(change.Item.Previous.Value);
                    set.Add(change.Item.Current);
                    break;
                case ListChangeReason.Remove:
                    set.Remove(change.Item.Current);
                    break;
                case ListChangeReason.RemoveRange:
                    set.RemoveRange(change.Range);
                    break;
                case ListChangeReason.Refresh:
                case ListChangeReason.Moved:
                    break;
                case ListChangeReason.Clear:
                    set.Clear();
                    break;
            }
        }
    }

    public static void ApplyChanges<TKey, TValue>(this IDictionary<TKey, TValue> dict, IChangeSet<TValue, TKey> changes)
        where TKey : notnull
        where TValue : notnull
    {
        foreach (var change in changes)
        {
            switch (change.Reason)
            {
                case ChangeReason.Add or ChangeReason.Update:
                    dict[change.Key] = change.Current;
                    break;
                case ChangeReason.Remove:
                    dict.Remove(change.Key);
                    break;
                case ChangeReason.Refresh:
                case ChangeReason.Moved:
                    break;
            }
        }
    }
}
