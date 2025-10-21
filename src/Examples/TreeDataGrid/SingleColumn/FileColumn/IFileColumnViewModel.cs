using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Helpers.TreeDataGrid;
using NexusMods.UI.Sdk;

namespace Examples.TreeDataGrid.SingleColumn.FileColumn;

public interface IFileColumnViewModel : IViewModelInterface, IExpandableItem, IDynamicDataTreeItem<IFileColumnViewModel, GamePath>
{
    /// <summary>
    ///     True if this node represents a file, else false.
    /// </summary>
    bool IsFile { get; }
    
    /// <summary>
    ///     Name of the file or folder segment.
    /// </summary>
    string Name { get; }
}
