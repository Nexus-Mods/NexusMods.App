using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;

namespace NexusMods.Extensions.DynamicData;

/// <summary/>
public static class EntityDictionaryExtensions
{
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
        where TOutK : notnull where TOutV : notnull
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
