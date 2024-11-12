using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Abstractions.Games;


/// <summary>
/// A factory for creating providers for sortable items for specific loadouts
/// </summary>
public interface ISortableItemProviderFactory
{
    /// <summary>
    /// Returns a provider of sortable items for a specific loadout
    /// </summary>
    ILoadoutSortableItemProvider GetLoadoutSortableItemProvider(LoadoutId loadoutId);
    
    
    /// <summary>
    /// Returns id of the type of the loadout
    /// </summary>
    Guid StaticSortOrderTypeId { get; }
}
