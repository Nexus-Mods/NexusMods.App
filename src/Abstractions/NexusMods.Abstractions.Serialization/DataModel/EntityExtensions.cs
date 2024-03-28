namespace NexusMods.Abstractions.Serialization.DataModel;

/// <summary>
/// Extension methods related to the processing of entities.
/// </summary>
public static class EntityExtensions
{
    /// <summary>
    /// Persists an entity to the store if it hasn't already been persisted, and returns it.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="store"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T WithPersist<T>(this T entity, IDataStore store) where T : Entity
    {
        entity.EnsurePersisted(store);
        return entity;
    }
    
    /// <summary>
    /// Ensures this item is stored in the database.
    /// </summary>
    public static void EnsureAllPersisted<T>(this Span<T> values, IDataStore store) where T : Entity
    {
        var ids = store.PutAll(values);
        for (var x = 0; x < ids.Length; x++)
            values[x].DataStoreId = ids[x];
    }

    /// <summary>
    /// Persists a collection of entities to the store if they haven't already been persisted, and returns them.
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="store"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IEnumerable<T> WithPersist<T>(this IEnumerable<T> entities, IDataStore store) where T : Entity
    {
        // TODO: Perf could be improved here by not using enumerables,
        // but bulk persist is a good starting point.
        var allEntities = entities.ToArray();

        var numUnpersisted = 0;
        var allUnpersisted = GC.AllocateUninitializedArray<T>(allEntities.Length);
        for (var x = 0; x < allUnpersisted.Length; x++)
        {
            // ReSharper disable once InvertIf , hot path
            if (!allEntities[x].IsPersisted)
            {
                allUnpersisted[x] = allEntities[x];
                numUnpersisted++;
            }
            else
            {
                // Yield the persistent elements first
                yield return allEntities[x];
            }
        }

        var itemsToPersist = allUnpersisted.AsSpan(0, numUnpersisted);
        itemsToPersist.EnsureAllPersisted(store);
        foreach (var entity in allUnpersisted)
            yield return entity;
    }
}
