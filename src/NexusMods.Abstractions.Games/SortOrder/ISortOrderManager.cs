using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// Represents the single central manager for load order related updates.
/// One instance per game.
/// </summary>
public interface ILoadOrderManager
{
    /// <summary>
    /// Should be acquired when changes to the sort order are being made.
    /// Sort order update operations often need to read the previous state to modify it.
    /// This lock should be used to ensure that no other operation is modifying the sort order in between reads and writes.
    /// </summary>
    internal ValueTask<IDisposable> Lock(CancellationToken token = default);
    
    /// <summary>
    /// Will update all the sort order for the given loadout and optionally for the given collection group.
    /// A game can have multiple sort order varieties, so this will update all of them.
    /// </summary>
    /// <returns></returns>
    public ValueTask UpdateLoadOrders(LoadoutId loadoutId, Optional<CollectionGroupId> collectionGroupId = default, CancellationToken token = default);
    
    /// <summary>
    /// Returns all the sort oder varieties for the game.
    /// One instance of ISortOrderVariety for each variety.
    /// </summary>
    /// <returns></returns>
    public ISortOrderVariety[] GetSortOrderVarieties();
}
