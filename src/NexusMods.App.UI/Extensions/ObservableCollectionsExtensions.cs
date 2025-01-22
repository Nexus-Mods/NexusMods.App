using System.Collections.Specialized;
using DynamicData.Kernel;
using ObservableCollections;
using R3;

namespace NexusMods.App.UI.Extensions;

public static class ObservableCollectionsExtensions
{
    public static Observable<KeyValuePair<TKey, TValue>> ObserveKeyChanges<TKey, TValue>(
        this IReadOnlyObservableDictionary<TKey, TValue> source,
        TKey key)
        where TKey : IEquatable<TKey>
    {
        return source
            .ObserveChanged()
            .Select(key, static Optional<KeyValuePair<TKey, TValue>> (ev, key) =>
            {
                if (ev.Action == NotifyCollectionChangedAction.Add && ev.NewItem.Key.Equals(key)) return ev.NewItem;
                if (ev.Action == NotifyCollectionChangedAction.Remove && ev.OldItem.Key.Equals(key)) return ev.OldItem;
                if (ev.Action == NotifyCollectionChangedAction.Replace)
                {
                    if (ev.NewItem.Key.Equals(key)) return ev.NewItem;
                    if (ev.OldItem.Key.Equals(key)) return ev.OldItem;
                }

                return Optional<KeyValuePair<TKey, TValue>>.None;
            })
            .Where(static optional => optional.HasValue)
            .Select(static optional => optional.Value);
    }
}
