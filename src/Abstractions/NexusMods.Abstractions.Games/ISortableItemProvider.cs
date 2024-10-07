using System.Collections.ObjectModel;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// A provider of sortable items, these are often things like game plugins or mods that the game can load natively.
/// </summary>
public interface ISortableItemProvider
{
    /// <summary>
    /// Get all the sortable items for the given loadout
    /// </summary>
    public IEnumerable<ISortableItem> GetItems(Loadout.ReadOnly loadout);
}
