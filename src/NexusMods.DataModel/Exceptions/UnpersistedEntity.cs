using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel.Exceptions;

public class UnpersistedEntity : Exception
{
    public UnpersistedEntity(Entity entity) : base(
        $"Entity {entity} is not persisted in the database, and has no ID.")
    { }
}
