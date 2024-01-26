using System.Reactive.Linq;
using DynamicData;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.Extensions.DynamicData;

namespace NexusMods.App.UI;

public static class DynamicDataExtensions
{
    /// <summary>
    /// Similar to <see cref="ObservableListEx.MergeMany{T,TDestination}"/>
    /// but it also passes the source item to the stream.
    /// </summary>
    public static IObservable<(TSource, TOut)> MergeManyWithSource<TSource, TOut>(
        this IObservable<IChangeSet<TSource>> source,
        Func<TSource, IObservable<TOut>> observableSelector) where TSource : notnull
    {
        return Observable.Create<(TSource, TOut)>(observer =>
        {
            var locker = new object();
            return source
                .SubscribeMany(item => observableSelector(item)
                    .Synchronize(locker)
                    .Subscribe(x => observer.OnNext((item, x)))
                )
                .Subscribe(_ => { }, observer.OnError);
        });
    }

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
}
