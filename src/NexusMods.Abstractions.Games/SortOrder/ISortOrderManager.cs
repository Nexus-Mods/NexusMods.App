using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// Represents the single central manager for load order related updates.
/// One instance per game.
/// </summary>
public interface ISortOrderManager
{
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
    public ReadOnlySpan<ISortOrderVariety> GetSortOrderVarieties();
    
    /// <summary>
    /// Sets the sort order varieties for the current game.
    /// Should only be called once during initialization of the game.
    /// </summary>
    /// <param name="sortOrderVarieties"></param>
    public void RegisterSortOrderVarieties(ISortOrderVariety[] sortOrderVarieties);
}
