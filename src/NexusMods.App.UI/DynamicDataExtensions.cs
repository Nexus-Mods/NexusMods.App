using System.Reactive.Linq;
using DynamicData;

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
}
