namespace NexusMods.DataModel.Abstractions;

public interface IHasEntityId<TId>
{
    public TId Id { get; }
}
