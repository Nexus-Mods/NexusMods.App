namespace NexusMods.DataModel.Abstractions;

public static class EntityExtensions
{
    public static T WithPersist<T>(this T entity, IDataStore store) where T : Entity
    {
        entity.EnsurePersisted(store);
        return entity;
    }

    public static IEnumerable<T> WithPersist<T>(this IEnumerable<T> entities, IDataStore store) where T : Entity
    {
        foreach (var entity in entities)
        {
            entity.EnsurePersisted(store);
            yield return entity;
        }
    }
}
