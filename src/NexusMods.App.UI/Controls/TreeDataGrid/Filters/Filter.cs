using NexusMods.Paths;

namespace NexusMods.App.UI.Controls.TreeDataGrid.Filters;

/// <summary>
/// Base filter type using discriminated union pattern for TreeDataGrid filtering.
/// </summary>
public abstract record Filter
{
    /// <summary>
    /// Filter by name/text content (case-insensitive substring matching by default).
    /// </summary>
    public sealed record NameFilter(string SearchText, bool CaseSensitive = false) : Filter;
    
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
} 