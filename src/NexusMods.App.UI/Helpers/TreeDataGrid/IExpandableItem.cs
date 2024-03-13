namespace NexusMods.App.UI.Helpers.TreeDataGrid;

/// <summary>
///     Represents a ViewModel that can be expanded.
/// </summary>
public interface IExpandableItem
{
    /// <summary>
    ///     Whether the node is expanded in the UI.
    /// </summary>
    public bool IsExpanded { get; set; }
}
