
using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// An abstraction for a sortable item that can be moved around in a list relative to its siblings.
/// All items in the list will have a non-gaming sort index. If an item is moved the other items will
/// adjust to compensate for the positional change.
///
/// Non-generic interface for the UI.
/// </summary>
public interface IReactiveSortItem
{
    /// <summary>
    /// Represents a game-specific id for the item, ideally what the game uses to identify the items, often a path
    /// </summary>
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
    /// The optional loadout group id of the mod containing the item.
    /// Optional since some items my not be part of a loadout group.
    /// </summary>
    public Optional<LoadoutItemGroupId> ModGroupId { get; set; }
    
    /// <summary>
    /// Represents whether the item is active in the sort order or not
    /// An item is considered active if it is part of the sort order and will be loaded by the game
    /// An item is considered inactive if it is for some reason not going to be loaded by the game,
    /// e.g. it is disabled in the sort order, or parent mod is disabled.
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Contains the loadout-specific data for the item, such as parent mod or enabled state.
    /// </summary>
    public ISortItemLoadoutData? LoadoutData { get; set; }
}

/// <summary>
/// <inheritdoc cref="IReactiveSortItem"/>
/// Generic version of the sortable item interface for use in provider implementations.
/// </summary>
public interface IReactiveSortItem<in TSelf, out TKey> : IComparable<TSelf>, IReactiveSortItem
    where TSelf : IReactiveSortItem<TSelf, TKey>
    where TKey : IEquatable<TKey>, ISortItemKey
{
    /// <inheritdoc />
    ISortItemKey IReactiveSortItem.Key => Key;

    /// <summary>
    /// <inheritdoc cref="IReactiveSortItem.Key" />
    /// Generic version of the key property
    /// </summary>
    public new TKey Key { get; }

    int IComparable<TSelf>.CompareTo(TSelf? other)
    {
        if (other == null) return 1;
        return SortIndex.CompareTo(other.SortIndex);
    }
}
