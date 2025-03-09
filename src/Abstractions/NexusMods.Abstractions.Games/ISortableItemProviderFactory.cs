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
    /// Display name for this sort order type
    /// </summary>
    string SortOrderName { get; }
    
    /// <summary>
    /// Short descriptive title for the load order, describing the override behavior of the sort order
    /// </summary>
    /// <example>
    /// "Last Loaded plugin Wins"
    /// </example>
    /// <remarks>
    /// Avoid using "higher" or "lower" terms, as the index numbers can be sorted both in ascending or descending order,
    /// making their meaning ambiguous.
    /// </remarks>
    string SortOrderHeading { get; }
    
    /// <summary>
    /// Heading for more details load order override information
    /// </summary>
    /// <example>
    /// "Load Order for REDmods in Cyberpunk 2077 - First Loaded Wins"
    /// </example>
    string OverrideInfoTitle { get; }
    
    /// <summary>
    /// Detailed description of the load order and its override behavior
    /// </summary>
    string OverrideInfoMessage { get; }
    
    /// <summary>
    /// Short tooltip message to explain the winning index number in the load order
    /// </summary>
    string WinnerIndexToolTip { get; }
    
    /// <summary>
    /// Header text for the index column
    /// </summary>
    string IndexColumnHeader { get; }
    
    /// <summary>
    /// Header text for the name column
    /// </summary>
    string NameColumnHeader { get; }
    
    /// <summary>
    /// Title text to display in case there are no sortable items to sort
    /// </summary>
    string EmptyStateMessageTitle { get; }
    
    /// <summary>
    /// Contents text to display in case there are no sortable items to sort
    /// </summary>
    string EmptyStateMessageContents { get; }
    
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
}


