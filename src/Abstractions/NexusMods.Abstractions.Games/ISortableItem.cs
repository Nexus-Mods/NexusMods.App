
namespace NexusMods.Abstractions.Games;

public interface ISortableItem
{
    public ISortItemKey Key { get; }
    
    /// <summary>
    /// The index of the item in a sorted list of item as given by the provider
    /// </summary>
    public int SortIndex { get; set; }
    
    /// <summary>
    /// Name of the item for display purposes
    /// </summary>
    public string DisplayName { get; }
    
    /// <summary>
    /// The name of the winning mod containing the item
    /// </summary>
    public string ModName { get; set; }
    
    /// <summary>
    /// Represents whether the item is active in the sort order or not
    /// An item is considered active if it is part of the sort order and will be loaded by the game
    /// An item is considered inactive if it is for some reason not going to be loaded by the game,
    /// e.g. it is disabled in the sort order, or parent mod is disabled.
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// An abstraction for a sortable item that can be moved around in a list relative to its siblings.
/// All items in the list will have a non-gaming sort index. If a item is moved the other items will
/// adjust to compensate for the positional change.
/// </summary>
public interface ISortableItem<TSelf, TKey> : IComparable<TSelf>, ISortableItem
    where TSelf : ISortableItem<TSelf, TKey>
    where TKey : IEquatable<TKey>, ISortItemKey
{
    ISortItemKey ISortableItem.Key => Key;

    /// <summary>
    /// Represents a game-specific id for the item, ideally what the game uses to identify the items, often a path
    /// </summary>
    public TKey Key { get; }

    int IComparable<TSelf>.CompareTo(TSelf? other)
    {
        if (other == null) return 1;
        return SortIndex.CompareTo(other.SortIndex);
    }
}
