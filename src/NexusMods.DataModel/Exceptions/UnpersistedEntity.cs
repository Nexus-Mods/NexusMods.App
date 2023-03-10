namespace NexusMods.DataModel.Exceptions;

public class UnpersistedEntity : Exception
{
    public UnpersistedEntity() : base(
        $"Entity is not persisted in the database, and has no ID.")
    { }
}
