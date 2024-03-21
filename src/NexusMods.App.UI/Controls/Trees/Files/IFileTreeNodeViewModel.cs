using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Controls.Trees.Common;
using NexusMods.App.UI.Helpers;
using NexusMods.App.UI.Helpers.TreeDataGrid;

namespace NexusMods.App.UI.Controls.Trees.Files;

public interface IFileTreeNodeViewModel : IViewModelInterface, IExpandableItem, IDynamicDataTreeItem<IFileTreeNodeViewModel, GamePath>
{
    /// <summary>
    ///     The icon that's used to display this specific node.
    /// </summary>
    FileTreeNodeIconType Icon { get; set; }
    
    /// <summary>
    ///     True if this node represents a file.
    /// </summary>
    bool IsFile { get; }
    
    /// <summary>
    ///     Name of the file or folder segment.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    ///     The size of the file, in bytes.
    /// </summary>
    ulong FileSize { get; }

    /// <summary>
    ///     The key to the parent of this node.
    /// </summary>
    GamePath ParentKey { get; }
}
