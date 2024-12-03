using DynamicData;
using JetBrains.Annotations;
using ObservableCollections;
using R3;
using CompositeDisposable = R3.CompositeDisposable;

namespace NexusMods.Abstractions.UI.Extensions;

[PublicAPI]
public static class R3Extensions
{
    /// <summary>
    /// Provides an activation block for <see cref="ReactiveR3Object"/>.
    /// </summary>
    [MustDisposeResource] public static IDisposable WhenActivated<T>(
        this T obj,
        Action<T, CompositeDisposable> block)
        where T : IReactiveR3Object
    {
        return WhenActivated(obj, state: block, static (obj, block, disposables) =>
        {
            block(obj, disposables);
        });
    }

    /// <summary>
    /// Provides an activation block for <see cref="ReactiveR3Object"/>.
    /// </summary>
    [MustDisposeResource]
    public static IDisposable WhenActivated<T, TState>(
        this T obj,
        TState state,
        Action<T, TState, CompositeDisposable> block)
        where T : IReactiveR3Object
        where TState : notnull
    {
        var d = Disposable.CreateBuilder();

        var serialDisposable = new SerialDisposable();
        serialDisposable.AddTo(ref d);

        obj.Activation.DistinctUntilChanged().Subscribe(((obj, state), serialDisposable, block), onNext: static (isActivated, state) =>
        {
            var (wrapper, serialDisposable, block) = state;

            serialDisposable.Disposable = null;
            if (isActivated)
            {
                var compositeDisposable = new CompositeDisposable();
                serialDisposable.Disposable = compositeDisposable;

                block(wrapper.obj, wrapper.state, compositeDisposable);
            }
        }, onCompleted: static (_, state) =>
        {
            var (_, serialDisposable, _) = state;
            serialDisposable.Disposable = null;
        }).AddTo(ref d);

        return d.Build();
    }

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

    public static void ApplyChanges<TKey, TValue>(this ObservableList<TValue> list, IChangeSet<TValue, TKey> changes)
        where TValue : notnull
        where TKey : notnull
    {
        foreach (var change in changes)
        {
            switch (change.Reason)
            {
                case ChangeReason.Add:
                    if (change.CurrentIndex < 0)
                    {
                        list.Add(change.Current);
                    }
                    else
                    {
                        list.Insert(change.CurrentIndex, change.Current);
                    }
                    break;
                case ChangeReason.Remove:
                    list.Remove(change.Current);
                    break;
                case ChangeReason.Update:
                    var index = list.IndexOf(change.Previous.Value);
                    if (index != -1)
                    {
                        list[index] = change.Current;
                    }

                    break;
            }
        }
    }

    public static void ApplyChanges<TKey, TValue>(this ObservableHashSet<TValue> set, IChangeSet<TValue, TKey> changes)
        where TValue : notnull
        where TKey : notnull
    {
        foreach (var change in changes)
        {
            switch (change.Reason)
            {
                case ChangeReason.Add:
                    set.Add(change.Current);
                    break;
                case ChangeReason.Remove:
                    set.Remove(change.Current);
                    break;
                case ChangeReason.Update:
                    if (set.Remove(change.Previous.Value))
                    {
                        set.Add(change.Current);
                    }

                    break;
            }
        }
    }
}
