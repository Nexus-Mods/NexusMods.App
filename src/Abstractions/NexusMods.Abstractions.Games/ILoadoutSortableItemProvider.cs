using NexusMods.Abstractions.Loadouts;
using ObservableCollections;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// A loadout specific provider and manager of sortable items.
/// </summary>
public interface ILoadoutSortableItemProvider 
{
    /// <summary>
    /// The ISortableItemProviderFactory that created this provider
    /// </summary>
    public ISortableItemProviderFactory ParentFactory { get; }
    
    /// <summary>
    /// The id of the loadout that the sortable items are associated with
    /// </summary>
    public LoadoutId LoadoutId { get; }
    
    
    public ObservableList<ISortableItem> SortableItems { get; }
    
    /// <summary>
    /// Sets the relative position of a sortable item in the load order
    /// </summary>
    /// <param name="sortableItem">item to move</param>
    /// <param name="delta">positive or negative index delta</param>
    /// <returns></returns>
    Task SetRelativePosition(ISortableItem sortableItem, int delta);
}
