namespace NexusMods.Abstractions.DataModel.Entities;

/// <summary>
/// Interface used for items which provide an entity ID.
/// </summary>
/// <typeparam name="TId">Type of ID stored by the item.</typeparam>
public interface IHasEntityId<TId>
{
    /// <summary>
    /// ID stored by the item.
    /// </summary>
    public TId Id { get; }
}
