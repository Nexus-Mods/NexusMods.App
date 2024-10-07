using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// An abstraction for a sortable item that can be moved around in a list relative to its siblings.
/// All items in the list will have a non-gaming sort index. If a item is moved the other items will
/// adjust to compensate for the positional change.
/// </summary>
public interface ISortableItem
{
    /// <summary>
    /// The provider that contains all the items that can be sorted
    /// </summary>
    public ISortableItemProvider Provider { get; }
    
    /// <summary>
    /// The index of the item in a sorted list of item as given by the provider
    /// </summary>
    public int SortIndex { get; }
    
    /// <summary>
    /// This is used as the unique identifier for the entity.
    /// </summary>
    public EntityId EntityId { get; }
    
    /// <summary>
    /// Moves the item relative to other items by the sepecified amount
    /// </summary>
    public Task SetRelativePosition(int delta);
}
