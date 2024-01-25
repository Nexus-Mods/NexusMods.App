namespace NexusMods.Abstractions.Serialization.DataModel.Exceptions;

/// <summary>
/// Exception thrown when an entity is not persisted to the database.
/// </summary>
public class UnpersistedEntity : Exception
{
    /// <inheritdoc />
    public UnpersistedEntity() : base(
        $"Entity is not persisted in the database, and has no ID.")
    { }
}
