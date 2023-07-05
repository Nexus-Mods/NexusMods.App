using NexusMods.DataModel.Abstractions.Ids;

namespace NexusMods.DataModel.Abstractions;

/// <summary>
/// An abstraction used for storing key-value pairs
/// used throughout the application. Usually abstracts disk or database.
/// </summary>
public interface IDataStore
{
    // TODO: There's some potential perf wins here by devirtualising the ID(s). Low priority. More important optimisations to be done at time of writing.

    /// <summary>
    /// Places an individual entity inside the data store.
    /// </summary>
    /// <param name="value">The entity to be placed in the data store.</param>
    /// <typeparam name="T">The type of entity to be placed.</typeparam>
    /// <returns>Unique identifier of the entity.</returns>
    public IId Put<T>(T value) where T : Entity;

    /// <summary>
    /// Places an individual entity inside the data store.
    /// </summary>
    /// <param name="id">The unique ID to use for the value.</param>
    /// <param name="value">The entity to be placed in the data store.</param>
    /// <typeparam name="T">The type of entity to be placed.</typeparam>
    public void Put<T>(IId id, T value) where T : Entity;

    /// <summary>
    /// Retrieves an individual item with a specified ID from the data store.
    /// </summary>
    /// <param name="id">Unique identifier for the item.</param>
    /// <param name="canCache">True if this item can be cached, else false.</param>
    /// <typeparam name="T">Type of item to retrieve.</typeparam>
    /// <returns>Retrieved item from the data store, can be null.</returns>
    T? Get<T>(IId id, bool canCache = false) where T : Entity;

    /// <summary>
    /// Retrieves the raw data for an item with the given ID.
    /// </summary>
    /// <param name="id">Identifier for the item.</param>
    /// <returns>Raw data.</returns>
    byte[]? GetRaw(IId id);

    /// <summary>
    /// Places raw data for an item with a specified key into the data store.
    /// </summary>
    /// <param name="key">The key to use with the item.</param>
    /// <param name="val">The value to place inside the data store.</param>
    void PutRaw(IId key, ReadOnlySpan<byte> val);

    /// <summary>
    /// Performs a compare and swap operation on the given key. If the expected
    /// value matches the current value, the new value is set, and the method
    /// returns true. If the expected value does not match the current value,
    /// the new value is not set, and the method returns false. This method is
    /// atomic.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="val"></param>
    /// <param name="expected"></param>
    /// <returns></returns>
    bool CompareAndSwap(IId key, ReadOnlySpan<byte> val, ReadOnlySpan<byte> expected);

    /// <summary>
    /// Delete the value for the given id.
    /// </summary>
    /// <param name="id">Unique identifier for which to delete the value for.</param>
    void Delete(IId id);

    /// <summary>
    /// Places raw data for an item with a specified key into the data store.
    /// </summary>
    /// <param name="kvs">The key-value pairs to add to the data store.</param>
    /// <param name="token">Use this to cancel the operation at any time.</param>
    Task<long> PutRaw(IAsyncEnumerable<(IId Key, byte[] Value)> kvs, CancellationToken token = default);

    /// <summary>
    /// Retrieves a collection of items whose IDs begin with a specified prefix.
    /// </summary>
    /// <param name="prefix">The sequence of bytes/id that returned items should start with.</param>
    /// <typeparam name="T">The type of entity to store in the data store.</typeparam>
    /// <returns>Collection of items which start with the given prefix.</returns>
    IEnumerable<T> GetByPrefix<T>(IId prefix) where T : Entity;

    /// <summary>
    /// Allows you to subscribe to notifications for when the ID of an element
    /// is changed.
    /// </summary>
    IObservable<IId> IdChanges { get; }

    /// <summary>
    /// Returns all the IDs for a given Entity type.
    /// </summary>
    /// <returns></returns>
    IEnumerable<IId> AllIds(EntityCategory category);

    /// <summary>
    /// Serialize the entity the way it would be serialized in the data store and return the Hash as an Id64 and the
    /// serialized data. Put(entity) is equivalent to calling PutRaw(ContentHashId(data), data).
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="data"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Id64 ContentHashId<T>(T entity, out byte[] data) where T : Entity;
}
