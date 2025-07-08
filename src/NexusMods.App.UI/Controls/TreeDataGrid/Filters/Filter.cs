using System.ComponentModel.DataAnnotations;
using NexusMods.App.UI.Controls.TreeDataGrid.Filters;
using NexusMods.Paths;
namespace NexusMods.App.UI.Controls.Filters;

/// <summary>
/// Base filter type using discriminated union pattern for TreeDataGrid filtering.
/// </summary>
public abstract record Filter
{

    /// <summary>
    /// Returns true if the item model matches the filter, false otherwise.
    /// </summary>
    public virtual bool MatchesRow<TKey>(CompositeItemModel<TKey> itemModel) where TKey : notnull
    {
        // Default implementation: if any component matches the filter, the row matches
        foreach (var (_, component) in itemModel.Components)
        {
            var result = component.MatchesFilter(this);
            if (result == FilterResult.Pass)
                return true;
        }

        return this.MatchesChildren(itemModel); // Check if any child matches the filter
    }

    /// <summary>
    /// Returns true if any of the children matches the given filter, false otherwise.
    /// </summary>
    /// <remarks>
    /// This method forces all children models to be initialized and loaded, these will then stay loaded.
    /// This can result in a performance impact if many children are present.
    /// </remarks>
    private bool MatchesChildren<TKey>(CompositeItemModel<TKey> parentModel) where TKey : notnull
    {
        // This loads all the children and keeps them loaded even if they are not expanded, 
        // which is necessary for filtering, but may have a performance impact.
        foreach (var childModel in parentModel.InitAndGetChildren())
        {
            if (this.MatchesRow(childModel))
            {
                // If any child matches, the parent matches
                return true;
            }
        }
        return false; // No children matched
    }

    /// <summary>
    /// Filter by name/text content (case-insensitive substring matching by default).
    /// </summary>
    public sealed record NameFilter(string SearchText, bool CaseSensitive = false) : Filter;
    
    /// <summary>
    /// Filter by text content across multiple string-based components (case-insensitive substring matching by default).
    /// This filter matches text as displayed in the UI, so implementations must match what is actually shown to the user whenever possible.
    /// </summary>
    public sealed record TextFilter(string SearchText, bool CaseSensitive = false) : Filter;
    
    /// <summary>
    /// Filter by installation status.
    /// </summary>
    public sealed record InstalledFilter(bool ShowInstalled = true, bool ShowNotInstalled = true) : Filter;
    
    /// <summary>
    /// Filter by update availability.
    /// </summary>
    public sealed record UpdateAvailableFilter(bool ShowWithUpdates = true, bool ShowWithoutUpdates = true) : Filter;
    
    /// <summary>
    /// Filter by version pattern.
    /// </summary>
    public sealed record VersionFilter(string VersionPattern) : Filter;
    
    /// <summary>
    /// Filter by date range.
    /// </summary>
    public sealed record DateRangeFilter(DateTimeOffset StartDate, DateTimeOffset EndDate) : Filter;
    
    /// <summary>
    /// Filter by size range.
    /// </summary>
    public sealed record SizeRangeFilter(Size MinSize, Size MaxSize) : Filter;

    /// <summary>
    /// Logical AND of two filters (both must match).
    /// </summary>
    public sealed record AndFilter([Required, MinLength(2)] params Filter[] Filters) : Filter
    {
        public override bool MatchesRow<TKey>(CompositeItemModel<TKey> itemModel)
        {
            if (Filters.Length == 0)
                return new NoFilter().MatchesRow(itemModel);

            // If all of the inner filters match, the AND filter matches
            var parentMatches = true;
            foreach (var filter in Filters)
            {
                if (!filter.MatchesRow(itemModel))
                {
                    parentMatches = false; // If any filter fails, the AND filter fails
                    break;
                }
            }
            if (parentMatches)
                return true; // If parent matches, no need to check children

            return this.MatchesChildren(itemModel); // Check if any child matches the filter
        }
    }

    /// <summary>
    /// Logical OR of two filters (either can match).
    /// </summary>
    public sealed record OrFilter([Required, MinLength(2)] params Filter[] Filters) : Filter
    {
        public override bool MatchesRow<TKey>(CompositeItemModel<TKey> itemModel)
        {
            // No filters means no filtering, pass all items
            if (Filters.Length == 0)
                return new NoFilter().MatchesRow(itemModel); 
            
            // If any of the inner filters match, the OR filter matches
            foreach (var filter in Filters)
            {
                if (filter.MatchesRow(itemModel))
                {
                    return true;
                }
            }

            return this.MatchesChildren(itemModel); // Check if any child matches the filter
        }
    }

    /// <summary>
    /// Logical NOT of a filter (inverts the result).
    /// </summary>
    public sealed record NotFilter(Filter Inner) : Filter
    {
        public override bool MatchesRow<TKey>(CompositeItemModel<TKey> itemModel)
        {
            // Invert the result of the inner filter
            return !Inner.MatchesRow(itemModel);
        }
    }

    /// <summary>
    /// No filtering - passes all items through.
    /// </summary>
    public sealed record NoFilter() : Filter
    {
        public override bool MatchesRow<TKey>(CompositeItemModel<TKey> itemModel) => true;
    }

} 
