using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;

namespace NexusMods.DataModel.Extensions;

public static class ObservableExtensions
{
    /// <summary>
    /// Returns only values that are not null.
    /// Converts the nullability.
    /// </summary>
    /// <typeparam name="T">The type of value emitted by the observable.</typeparam>
    /// <param name="observable">The observable that can contain nulls.</param>
    /// <returns>A non nullable version of the observable that only emits valid values.</returns>
    public static IObservable<T> NotNull<T>(this IObservable<T?> observable) =>
        observable
            .Where(x => x is not null)
            .Select(x => x!);

    /// <summary>
    /// Compares the current collection with the previous collection and returns the changes.
    /// </summary>
    /// <param name="colls"></param>
    /// <typeparam name="TK"></typeparam>
    /// <typeparam name="TV"></typeparam>
    /// <returns></returns>
    public static IObservable<IChangeSet<IId, TK>> ToDiffedChangeSet<TK, TV>(
            this IObservable<EntityDictionary<TK, TV>> colls) where TK : notnull where TV : Entity
    {
        EntityDictionary<TK, TV>? old = null;

        return Observable.Create<IChangeSet<IId, TK>>(observer =>
        {
            return colls.Subscribe(coll =>
            {
                observer.OnNext(coll.Diff(old ?? EntityDictionary<TK, TV>.Empty(coll.Store)));
                old = coll;
            });
        });

    }

    /// <summary>
    /// Compares the current collection with the previous collection and returns the changes.
    /// Uses the key selector to determine the key, and the value selector to determine the value.
    /// </summary>
    /// <param name="colls"></param>
    /// <param name="keySelector"></param>
    /// <param name="valueSelector"></param>
    /// <typeparam name="TInV"></typeparam>
    /// <typeparam name="TOutV"></typeparam>
    /// <typeparam name="TOutK"></typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static IObservable<IChangeSet<TOutV, TOutK>>
        ToDiffedChangeSet<TInV, TOutV, TOutK>(
            this IObservable<IEnumerable<TInV>> colls,
            Func<TInV, TOutK> keySelector, Func<TInV, TOutV> valueSelector)
        where TOutK : notnull
    {
        var changeSet =
            new SourceCache<TOutV, TOutK>(x =>
                throw new InvalidOperationException());

        return Observable.Create<IChangeSet<TOutV, TOutK>>(observer =>
        {

            var disp1 = colls.Subscribe(coll =>
            {
                var indexed = coll.ToDictionary(keySelector, valueSelector);

                changeSet.Edit(x =>
                {
                    foreach (var (key, value) in indexed)
                    {
                        x.AddOrUpdate(value, key);
                    }

                    foreach (var (key, value) in x.KeyValues)
                    {
                        if (!indexed.ContainsKey(key))
                        {
                            x.Remove(key);
                        }
                    }
                });
            });

            var disp2 = changeSet.Connect().Subscribe(observer);

            return new CompositeDisposable(disp1, disp2);
        });

    }
}
