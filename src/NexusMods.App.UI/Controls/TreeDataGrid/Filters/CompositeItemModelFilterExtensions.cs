using JetBrains.Annotations;
using NexusMods.App.UI.Controls.Filters;
using NexusMods.App.UI.Pages.LibraryPage;

namespace NexusMods.App.UI.Controls.TreeDataGrid.Filters;

/// <summary>
/// Extension methods for filtering CompositeItemModel instances.
/// </summary>
[PublicAPI]
public static class CompositeItemModelFilterExtensions
{
    /// <summary>
    /// Evaluates if this model matches the given filter by checking its components.
    /// </summary>
    /// <param name="model">The composite item model to evaluate.</param>
    /// <param name="filter">The filter to apply.</param>
    /// <typeparam name="TKey">The key type of the model.</typeparam>
    /// <returns>True if the model matches the filter criteria.</returns>
    public static bool MatchesFilter<TKey>(this CompositeItemModel<TKey> model, Filter filter) where TKey : notnull
    {
        return filter switch
        {
            Filter.NoFilter => true,
            Filter.AndFilter(var left, var right) => model.MatchesFilter(left) && model.MatchesFilter(right),
            Filter.OrFilter(var left, var right) => model.MatchesFilter(left) || model.MatchesFilter(right),
            Filter.NotFilter(var inner) => !model.MatchesFilter(inner),
            Filter.TextFilter textFilter => model.MatchesTextFilter(textFilter),
            _ => model.MatchesAnyComponent(filter),
        };
    }
    
    /// <summary>
    /// Checks if any component in the model matches the filter using 3-state logic.
    /// </summary>
    /// <param name="model">The composite item model.</param>
    /// <param name="filter">The filter to evaluate.</param>
    /// <typeparam name="TKey">The key type of the model.</typeparam>
    /// <returns>True if the model should pass the filter.</returns>
    private static bool MatchesAnyComponent<TKey>(this CompositeItemModel<TKey> model, Filter filter) where TKey : notnull
    {
        foreach (var (_, component) in model.Components)
        {
            var result = component.MatchesFilter(filter);
            switch (result)
            {
                case FilterResult.Pass:
                    return true;
                case FilterResult.Fail:
                    return false;
                case FilterResult.Indeterminate:
                    continue; // This component does not support this filter. Go to next one.
            }
        }

        // If all components were indeterminate, don't filter out the item as a failsafe,
        // it simply means all components did not support this filter.
        return true;
    }
    
    /// <summary>
    /// Checks if any component in the model matches the text filter.
    /// If any component matches the TextFilter, then the item should pass the filter. Otherwise, it should not pass.
    /// </summary>
    /// <param name="model">The composite item model.</param>
    /// <param name="textFilter">The text filter to evaluate.</param>
    /// <typeparam name="TKey">The key type of the model.</typeparam>
    /// <returns>True if any component matches the text filter criteria.</returns>
    private static bool MatchesTextFilter<TKey>(this CompositeItemModel<TKey> model, Filter.TextFilter textFilter) where TKey : notnull
    {
        // Empty search text matches all items (empty string is a substring of any string)
        if (string.IsNullOrEmpty(textFilter.SearchText))
            return true;
            
        foreach (var (_, component) in model.Components)
        {
            var result = component.MatchesFilter(textFilter);
            if (result == FilterResult.Pass)
                return true;
        }

        // If no component matched the text filter, the item should not pass
        return false;
    }
} 
