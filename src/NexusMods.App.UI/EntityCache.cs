using System.Reactive.Linq;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions;


namespace NexusMods.App.UI;

/// <summary>
/// A in-memory entity cache and observable index for a MnemonicDb entity set
/// </summary>
/// <typeparam name="TKey">The type of the unique key for the items (often the model's Entity Id)</typeparam>
/// <typeparam name="TItem">The type of the entity being stored</typeparam>
/// <typeparam name="TIndex">The type of the index for the data</typeparam>
public class EntityCache<TKey, TItem, TIndex> : IDisposable
    where TKey : notnull
    where TItem : notnull
    where TIndex : notnull
{
    private record IndexRow(HashSet<IObserver<IChangeSet<TItem, TKey>>> Observers, Dictionary<TKey, TItem> Items);
    
    private readonly Dictionary<TIndex, IndexRow> _index = new();
    private readonly IDisposable _disposable;
    private readonly Lock _lock = new();
    private readonly Func<TItem,TIndex> _indexSelector;
    
    /// <summary>
    /// DI Constructor, must provide a factory for the observables, and an index selector
    /// </summary>
    /// <param name="connection">MnemonicDB Connection</param>
    /// <param name="indexSelector">A selector function for index keys. Multiple rows are allowed the same index </param>
    /// <param name="observableFactory">A factory for all items to observe given a MnemonicDB connection</param>
    public EntityCache(IConnection connection, Func<TItem, TIndex> indexSelector, Func<IConnection, IObservable<IChangeSet<TItem, TKey>>> observableFactory)
    {
        _indexSelector = indexSelector;
        _disposable = observableFactory(connection)
            .Subscribe(OnUpdate);

    }

    private void OnUpdate(IChangeSet<TItem, TKey> changes)
    {
        lock (_lock)
        {
            foreach (var change in changes)
            {
                var index = _indexSelector(change.Current);
                if (!_index.TryGetValue(index, out var row))
                {
                    row = new IndexRow(new HashSet<IObserver<IChangeSet<TItem, TKey>>>(), new Dictionary<TKey, TItem>());
                    _index.Add(index, row);
                }

                foreach (var observer in row.Observers)
                {
                    observer.OnNext(new ChangeSet<TItem, TKey>([change]));
                    
                }

                switch (change.Reason)
                {
                    case ChangeReason.Add:
                    case ChangeReason.Update:
                        row.Items[change.Key] = change.Current;
                        break;
                    case ChangeReason.Remove:
                        row.Items.Remove(change.Key);
                        break;
                }
            }
            
        }
    }
    
    /// <summary>
    /// Get an observable changeset for all the items in a specific index entry. 
    /// </summary>
    public IObservable<IChangeSet<TItem, TKey>> Get(TIndex index)
    {
        return Observable.Create<IChangeSet<TItem, TKey>>(observer =>
            {
                lock (_lock)
                {
                    if (!_index.TryGetValue(index, out var row))
                    {
                        row = new IndexRow(new HashSet<IObserver<IChangeSet<TItem, TKey>>>(), new Dictionary<TKey, TItem>());
                        _index.Add(index, row);
                    }

                    row.Observers.Add(observer);
                    observer.OnNext(new ChangeSet<TItem, TKey>(row.Items.Select(kvp => new Change<TItem, TKey>(ChangeReason.Add, kvp.Key, kvp.Value))));
                }

                return () =>
                {
                    lock (_lock)
                    {
                        if (_index.TryGetValue(index, out var row))
                        {
                            row.Observers.Remove(observer);
                        }
                    }
                };

            }
        );
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposable.Dispose();
    }
}
