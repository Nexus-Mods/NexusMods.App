using System.Reactive.Subjects;

namespace NexusMods.DataModel.Abstractions;

public class Root<TRoot> where TRoot : Entity
{
    private readonly Subject<(TRoot Old, TRoot New)> _changes = new();
    private readonly object _lockObject = new();

    /// <summary>
    /// Current value of the root instance
    /// </summary>
    public TRoot Value { get; private set; }
    
    
    /// <summary>
    /// Datastore used by this root when creating new entities
    /// </summary>
    public IDataStore Store { get; private set; }
    
    /// <summary>
    /// A list of all changes to this root. Values here are published outside of the locking
    /// semantics of this system, so changes may be received out-of-order of a large number of
    /// updates are happening and once from multiple threads. This can be avoided by performing
    /// multi-threaded code inside of the `Alter` function instead of having multiple threads
    /// call `Alter` at once.
    /// </summary>
    public IObservable<(TRoot Old, TRoot New)> Changes => _changes;

    public Root(TRoot registry)
    {
        Value = registry;
        Store = registry.Store;
    }

    /// <summary>
    /// Transactionally modify the contents of this root. The body of `f` may be executed
    /// several times if the contents of this `Root` instance has during the execution of `f`
    /// </summary>
    /// <param name="f"></param>
    public void Alter(Func<TRoot, TRoot> f)
    {
        using var _ = IDataStore.WithCurrent(Store);
        restart:
        var oldRoot = Value;
        var newRoot = f(oldRoot);
        if (newRoot.Id == oldRoot.Id)
            return;

        lock (_lockObject)
        {
            if (oldRoot.Id != Value.Id) goto restart;
            Value = newRoot;
        }
        _changes.OnNext((oldRoot, newRoot));
    }
    
    /// <summary>
    /// Transactionally modify the contents of this root. The body of `f` may be executed
    /// several times if the contents of this `Root` instance has during the execution of `f`
    /// </summary>
    /// <param name="f"></param>
    public async Task AlterAsync(Func<TRoot, ValueTask<TRoot>> f)
    {
        using var _ = IDataStore.WithCurrent(Store);
        restart:
        var oldRoot = Value;
        var newRoot = await f(oldRoot);
        if (newRoot.Id == oldRoot.Id)
            return;
        
        lock (_lockObject)
        {
            if (oldRoot.Id != Value.Id) goto restart;
            Value = newRoot;
        }
        _changes.OnNext((oldRoot, newRoot));
    }

    /// <summary>
    /// Hard re-sets the value of this root to a new value.
    /// </summary>
    /// <param name="newRoot"></param>
    public void Rebase(TRoot newRoot)
    {
        TRoot oldRoot;
        lock (_lockObject)
        {
            oldRoot = Value;
            Value = newRoot;
        }
        _changes.OnNext((oldRoot, newRoot));
    }
}