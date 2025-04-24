using System.ComponentModel;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// A factory for creating providers for sortable items for specific loadouts
/// </summary>
public interface ISortableItemProviderFactory : IDisposable
{
    /// <summary>
    /// Returns a provider of sortable items for a specific loadout
    /// </summary>
    ILoadoutSortableItemProvider GetLoadoutSortableItemProvider(LoadoutId loadoutId);
    
    /// <summary>
    /// Returns id of the type of the loadout
    /// </summary>
    Guid SortOrderTypeId { get; }
    
    /// <summary>
    /// Default direction (ascending/descending) in which sortIndexes should be sorted and displayed
    /// </summary>
    /// <remarks>
    /// Usually ascending, but could be different depending on what the community prefers and is used to
    /// </remarks>
    ListSortDirection SortDirectionDefault { get; }
    
    /// <summary>
    /// Defines whether smaller or greater index numbers win in case of conflicts between items in sorting order
    /// </summary>
    IndexOverrideBehavior IndexOverrideBehavior { get; }

    /// <summary>
    /// Contains UI strings and metadata for the sort order type
    /// </summary>
    SortOrderUiMetadata SortOrderUiMetadata { get; }
}


