using NexusMods.App.UI.Controls.TreeDataGrid.Filters;
using NexusMods.Paths;
namespace NexusMods.App.UI.Controls.Filters;

/// <summary>
/// Base filter type using discriminated union pattern for TreeDataGrid filtering.
/// </summary>
public abstract record Filter
{
    /// <summary>
    /// Tests a value against the filter
    /// </summary>
    public virtual FilterResult Match<T>(T value)
    {
        return FilterResult.Indeterminate;
    }

    /// <summary>
    /// Filter by name/text content (case-insensitive substring matching by default).
    /// </summary>
    public sealed record NameFilter(string SearchText, bool CaseSensitive = false) : Filter
    {
        public override FilterResult Match<T>(T value)
        {
            if (value is not string s)
                return FilterResult.Indeterminate;
            return s.Contains(SearchText, CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)
                ? FilterResult.Pass : FilterResult.Fail;
        }
    }

    /// <summary>
    /// Filter by text content across multiple string-based components (case-insensitive substring matching by default).
    /// This filter matches text as displayed in the UI, so implementations must match what is actually shown to the user whenever possible.
    /// </summary>
    public sealed record TextFilter(string SearchText, bool CaseSensitive = false) : Filter
    {
        public override FilterResult Match<T>(T value)
        {
            var strVal = value?.ToString();

            if (strVal is null)
                return FilterResult.Indeterminate;
            
            return strVal.Contains(SearchText, CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) ?
                FilterResult.Pass : FilterResult.Fail;
        }
    }
    
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
    public sealed record VersionFilter(string VersionPattern) : Filter
    {
        public override FilterResult Match<T>(T value)
        {
            if (value is string s)
                return s.Contains(VersionPattern, StringComparison.OrdinalIgnoreCase) ? FilterResult.Pass : FilterResult.Fail;
            return FilterResult.Indeterminate;
        }
    }

    /// <summary>
    /// Filter by date range.
    /// </summary>
    public sealed record DateRangeFilter(DateTimeOffset StartDate, DateTimeOffset EndDate) : Filter
    {
        public override FilterResult Match<T>(T value)
        {
            if (value is not DateTimeOffset date)
                return FilterResult.Indeterminate;
            return date >= StartDate && date <= EndDate ? FilterResult.Pass : FilterResult.Fail;
        }
    }

    /// <summary>
    /// Filter by size range.
    /// </summary>
    public sealed record SizeRangeFilter(Size MinSize, Size MaxSize) : Filter
    {
        public override FilterResult Match<T>(T value)
        {
            if (value is not Size size)
                return FilterResult.Indeterminate;
            return size >= MinSize && size <= MaxSize ? FilterResult.Pass : FilterResult.Fail;
        }
    }
    
    /// <summary>
    /// Logical AND of two filters (both must match).
    /// </summary>
    public sealed record AndFilter(Filter Left, Filter Right) : Filter;
    
    /// <summary>
    /// Logical OR of two filters (either can match).
    /// </summary>
    public sealed record OrFilter(Filter Left, Filter Right) : Filter;
    
    /// <summary>
    /// Logical NOT of a filter (inverts the result).
    /// </summary>
    public sealed record NotFilter(Filter Inner) : Filter;
    
    /// <summary>
    /// No filtering - passes all items through.
    /// </summary>
    public sealed record NoFilter() : Filter;
    
    // Static helper methods using C# 14 params span that chain existing filter types
    
    /// <summary>
    /// Creates a chained AND filter from multiple filters using C# 14 params span.
    /// Chains filters using the existing AndFilter type: And(a,b,c) becomes AndFilter(a, AndFilter(b, c)).
    /// </summary>
    /// <param name="filters">The filters to combine with AND logic.</param>
    /// <returns>A chained AND filter, or NoFilter if no filters provided, or the single filter if only one provided.</returns>
    public static Filter And(params ReadOnlySpan<Filter> filters)
    {
        return filters.Length switch
        {
            0 => new NoFilter(),
            1 => filters[0],
            2 => new AndFilter(filters[0], filters[1]),
            _ => ChainWithAnd(filters),
        };
    }

    private static Filter ChainWithAnd(ReadOnlySpan<Filter> filters)
    {
        var result = filters[^1]; // Start with the last filter
        for (var x = filters.Length - 2; x >= 0; x--)
            result = new AndFilter(filters[x], result);

        return result;
    }
    
    /// <summary>
    /// Creates a chained OR filter from multiple filters using C# 14 params span.
    /// Chains filters using the existing OrFilter type: Or(a,b,c) becomes OrFilter(a, OrFilter(b, c)).
    /// </summary>
    /// <param name="filters">The filters to combine with OR logic.</param>
    /// <returns>A chained OR filter, or NoFilter if no filters provided, or the single filter if only one provided.</returns>
    public static Filter Or(params ReadOnlySpan<Filter> filters)
    {
        return filters.Length switch
        {
            0 => new NoFilter(),
            1 => filters[0],
            2 => new OrFilter(filters[0], filters[1]),
            _ => ChainWithOr(filters),
        };
    }
    
    private static Filter ChainWithOr(ReadOnlySpan<Filter> filters)
    {
        var result = filters[^1]; // Start with the last filter
        for (var x = filters.Length - 2; x >= 0; x--)
            result = new OrFilter(filters[x], result);

        return result;
    }
} 
