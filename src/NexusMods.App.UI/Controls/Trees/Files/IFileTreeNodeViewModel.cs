using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.App.UI.Controls.Trees.Common;
using NexusMods.App.UI.Helpers.TreeDataGrid;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Resources;

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
    ulong FileSize { get; internal set; }

    /// <summary>
    ///     Total number of files descending from this node.
    /// </summary>
    uint FileCount { get; internal set; }

    /// <summary>
    ///     The change status of the file (for diff views).
    /// </summary>
    FileChangeType ChangeType { get; internal set; }

    /// <summary>
    ///     The string representation of the file count, empty string if 0.
    /// </summary>
    /// <returns></returns>
    string ToFormattedFileCount()
    {
        return FileCount > 0 ? FileCount.ToString() : string.Empty;
    }

    string ToFormattedChangeState()
    {
        return ChangeType switch
        {
            FileChangeType.Added => Language.IFileTreeNodeViewModel_ToFormattedChangeState_Added,
            FileChangeType.Modified => IsFile
                ? Language.IFileTreeNodeViewModel_ToFormattedChangeState_Modified
                : Language.IFileTreeNodeViewModel_ToFormattedChangeState_Contents_modified,
            FileChangeType.Removed => Language.IFileTreeNodeViewModel_ToFormattedChangeState_Removed,
            FileChangeType.None => string.Empty,
        };
    }

    /// <summary>
    ///     The key to the parent of this node.
    /// </summary>
    GamePath ParentKey { get; }
}
