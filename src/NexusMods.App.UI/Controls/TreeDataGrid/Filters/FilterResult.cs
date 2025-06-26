namespace NexusMods.App.UI.Controls.TreeDataGrid.Filters;

/// <summary>
/// Represents the result of applying a filter to a component.
/// </summary>
public enum FilterResult
{
    /// <summary>
    /// The component has no opinion on this filter (default case).
    /// If all components return this, the item should not be filtered out.
    /// </summary>
    Indeterminate,
    
    /// <summary>
    /// The component actively matches the filter criteria.
    /// The item should pass the filter.
    /// </summary>
    Pass,
    
    /// <summary>
    /// The component actively rejects the filter criteria.
    /// The item should be filtered out.
    /// </summary>
    Fail
} 