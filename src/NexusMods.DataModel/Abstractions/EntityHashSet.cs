using System.Collections;
using System.Collections.Immutable;
using NexusMods.DataModel.Abstractions.Ids;

namespace NexusMods.DataModel.Abstractions;

/// <summary>
/// Provides a abstraction over an underlying Data Store (<see cref="IDataStore"/>)
/// which allows you to quickly verify the existence.
/// </summary>
public struct EntityHashSet<T> : IEmptyWithDataStore<EntityHashSet<T>>,
    IEnumerable<T>,
    IEquatable<EntityHashSet<T>>,
    IWalkable<Entity>
    where T : Entity
{
    // Note: Items are currently persisted through calls to .DataStoreId ; but this will probably be improved.

    /// <inheritdoc />
    public static EntityHashSet<T> Empty(IDataStore store) => new(store);

    private readonly ImmutableHashSet<IId> _coll;
    private readonly IDataStore _store;

    /// <summary>
    /// Creates a new instance
    /// with a given backing data store.
    /// </summary>
    /// <param name="store">The store in which the resulting data is held.</param>
    public EntityHashSet(IDataStore store)
    {
        _coll = ImmutableHashSet<IId>.Empty;
        _store = store;
    }

    /// <summary>
    /// Creates a new hash set from the existing set.
    /// </summary>
    /// <param name="store">The store to which all of the data is written to.</param>
    /// <param name="ids">The IDs to be stored within this hashset.</param>
    public EntityHashSet(IDataStore store, IEnumerable<IId> ids)
    {
        _coll = ImmutableHashSet.CreateRange(ids);
        _store = store;
    }

    private EntityHashSet(IDataStore store, ImmutableHashSet<IId> coll)
    {
        _coll = coll;
        _store = store;
    }

    /// <summary>
    /// Returns the IDs of all of the items stored in this collection.
    /// </summary>
    public IEnumerable<IId> Ids => _coll;

    /// <summary>
    /// Returns the number of elements stored in this collection.
    /// </summary>
    public int Count => _coll.Count;

    /// <summary>
    /// Adds an item to the hashset; returning a new hashset.
    /// </summary>
    /// <param name="items">The items to add to the HashSet.</param>
    /// <returns>A new HashSet with the item present in its collection.</returns>
    public EntityHashSet<T> With(IEnumerable<T> items)
    {
        var builder = _coll.ToBuilder();
        foreach (var item in items)
            builder.Add(item.DataStoreId);

        return new EntityHashSet<T>(_store, builder.ToImmutable());
    }

    /// <summary>
    /// Removes the provided collection of elements from the set.
    /// </summary>
    /// <param name="val">The item to remove from the hash set.</param>
    public EntityHashSet<T> With(T val)
    {
        return new EntityHashSet<T>(_store, _coll.Add(val.DataStoreId));
    }
    /// <summary>
    /// Removes the provided collection of elements from the set.
    /// </summary>
    /// <param name="val">The item to remove from the hash set.</param>
    public EntityHashSet<T> Without(T val)
    {
        return new EntityHashSet<T>(_store, _coll.Remove(val.DataStoreId));
    }

    /// <summary>
    /// Removes the provided collection of elements from the set.
    /// </summary>
    /// <param name="vals">The values to remove from the hash set.</param>
    public EntityHashSet<T> Without(IEnumerable<T> vals)
    {
        var newColl = vals.Aggregate(_coll, (acc, itm) => acc.Remove(itm.DataStoreId));
        return new EntityHashSet<T>(_store, newColl);
    }

    /// <summary>
    /// Transforms this hashset returning a new hashset. If `func` returns null for a given
    /// value, the item is removed from the set.
    /// </summary>
    public EntityHashSet<T> Keep(Func<T, T?> func)
    {
        var coll = _coll;
        foreach (var id in _coll)
        {
            var result = func((T)_store.Get<Entity>(id)!);
            if (result == null)
            {
                coll = coll.Remove(id);
            }
            else if (!result.DataStoreId.Equals(id))
                coll = coll.Remove(id).Add(result.DataStoreId);
        }
        return ReferenceEquals(coll, _coll) ? this : new EntityHashSet<T>(_store, coll);
    }

    /// <summary>
    /// Verifies whether an item is contained within this hashset.
    /// </summary>
    /// <param name="val">The value.</param>
    public bool Contains(T val)
    {
        return _coll.Contains(val.DataStoreId);
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        foreach (var itm in _coll)
            yield return (T)_store.Get<Entity>(itm)!;
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public bool Equals(EntityHashSet<T> other)
    {
        if (other.Count != Count) return false;
        foreach (var itm in other._coll)
            if (!_coll.Contains(itm))
                return false;

        return true;
    }

    /// <inheritdoc />
    public TState Walk<TState>(Func<TState, Entity, TState> visitor, TState initial)
    {
        return this.Aggregate(initial, visitor);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is EntityHashSet<T> other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(_coll, _store);
    }
}
