namespace NexusMods.DataModel.Abstractions;

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
    /// Persists a collection of entities to the store if they haven't already been persisted, and returns them.
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="store"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IEnumerable<T> WithPersist<T>(this IEnumerable<T> entities, IDataStore store) where T : Entity
    {
        foreach (var entity in entities)
        {
            entity.EnsurePersisted(store);
            yield return entity;
        }
    }
}
