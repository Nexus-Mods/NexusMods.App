using System.Reactive.Subjects;
using NexusMods.DataModel.Abstractions.Ids;

namespace NexusMods.DataModel.Abstractions;

/// <summary>
/// Represents a named pointer to the first element of a tree of nodes (e.g. <see cref="Loadouts"/>).
/// </summary>
/// <typeparam name="TRoot">Type of entity represented by the root.</typeparam>
/// <remarks>
///     An entity root can be thought to be like a GC Root.
///     Any element not reachable from the current root is considered 'dead' and
///     thus eligible to be removed from the data store at some point in the future.
/// </remarks>
public class Root<TRoot> where TRoot : Entity, IEmptyWithDataStore<TRoot>
{
    // TODO: Potentially incorrect equals check here.
    private readonly Subject<(TRoot Old, TRoot New)> _changes = new();

    private EntityLink<TRoot> _root;

    /// <summary>
    /// Retrieves the value of this root.
    /// </summary>
    public TRoot Value => _root.Id == IdEmpty.Empty ? TRoot.Empty(Store) : _root.Value;

    /// <summary>
    /// Datastore used by this root when creating new entities.
    /// </summary>
    public IDataStore Store { get; private set; }

    /// <summary>
    /// The item type this root currently represents.
    /// </summary>
    public RootType Type { get; private set; }

    /// <summary>
    /// A list of all changes to this root. Values here are published outside of the locking
    /// semantics of this system, so changes may be received out-of-order of a large number of
    /// updates are happening and once from multiple threads. This can be avoided by performing
    /// multi-threaded code inside of the `Alter` function instead of having multiple threads
    /// call `Alter` at once.
    /// </summary>
    public IObservable<(TRoot Old, TRoot New)> Changes => _changes;

    /// <summary>
    /// Creates a new root with a certain backing data store.
    /// </summary>
    /// <param name="type">Type of element stored by this root.</param>
    /// <param name="store">Data store behind which this root will be stored.</param>
    public Root(RootType type, IDataStore store)
    {
        Type = type;
        Store = store;

        var initRoot = Store.GetRoot(type);
        _root = new EntityLink<TRoot>(initRoot ?? IdEmpty.Empty, store);
    }

    /// <summary>
    /// Transactionally modify the contents of this root. The body of `f` may be executed
    /// several times if the contents of this `Root` instance has changed during the execution of `f`
    /// </summary>
    /// <param name="f"></param>
    public void Alter(Func<TRoot, TRoot> f)
    {
        var oldId = Store.GetRoot(Type);
        if (oldId != null && oldId != _root.Id)
            _root = new EntityLink<TRoot>(oldId, Store);

        restart:
        var oldRoot = _root.Id == IdEmpty.Empty ? TRoot.Empty(Store) : _root.Value;

        var newRoot = f(oldRoot);
        if (newRoot.DataStoreId == oldRoot.DataStoreId)
            return;

        if (!Store.PutRoot(Type, _root.Id, newRoot.DataStoreId))
        {
            var newId = Store.GetRoot(Type)!;
            _root = new EntityLink<TRoot>(newId, Store);
            goto restart;
        }

        _root = new EntityLink<TRoot>(newRoot.DataStoreId, Store);

        _changes.OnNext((oldRoot, newRoot));
    }
}
