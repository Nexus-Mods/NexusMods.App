using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// An abstraction for loadout-specific data of an item that is part of a sort order.
/// While <see cref="SortableEntry"/> represents an item in a sort order, it doesn't include information regarding the associated loadoutItem or mod.
/// This encapsulates the loadout-specific parts of data, like active state, without the sort index.
/// </summary>
public interface ILoadoutSortItem
{
    /// <summary>
    /// Represents a game-specific id for the item, ideally what the game uses to identify the items, often a path
    /// </summary>
    public ISortItemKey Key { get; }
    
    /// <summary>
    /// Represents whether the item is enabled in the loadout or not.
    /// An item might be enabled in the loadout but not active in the sort order.
    /// </summary>
    public bool IsEnabled { get; set; }
    
    /// <summary>
    /// The name of the winning mod containing the item.
    /// </summary>
    string ModName { get; set; }
    
    /// <summary>
    /// The optional loadout group id of the mod containing the item.
    /// Optional since some items my not be part of a loadout group.
    /// </summary>
    public Optional<LoadoutItemGroupId> ModGroupId { get; set; }
}
