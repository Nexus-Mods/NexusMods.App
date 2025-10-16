using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Files.Diff;
using NexusMods.App.UI.Controls.Trees.Common;
using NexusMods.App.UI.Helpers.TreeDataGrid;
using NexusMods.App.UI.Resources;
using NexusMods.UI.Sdk;

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
    ///     True if this node represents a file deletion.
    /// </summary>
    bool IsDeletion { get; }

    /// <summary>
    ///     Boolean value for FileChangeType.Added. Used for setting style classes.
    /// </summary>
    bool IsChangeAdded => ChangeType == FileChangeType.Added;
    
    /// <summary>
    ///     Boolean value for FileChangeType.Modified. Used for setting style classes.
    /// </summary>
    bool IsChangeModified => ChangeType == FileChangeType.Modified;
    
    /// <summary>
    ///     Boolean value for FileChangeType.Removed. Used for setting style classes.
    /// </summary>
    bool IsChangeRemoved => ChangeType == FileChangeType.Removed;

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
        return FileCount > 0 ? FileCount.ToString("N0") : string.Empty;
    }

    string FormattedChangeState => ToFormattedChangeState();

    string FormattedChangeStateToolTip => ToFormattedChangeStateToolTip();
    
    /// <summary>
    ///     The key to the parent of this node.
    /// </summary>
    GamePath ParentKey { get; }
    
    private string ToFormattedChangeState()
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

    private string ToFormattedChangeStateToolTip()
    {
        return ChangeType switch
        {
            FileChangeType.Added => Language.IFileTreeNodeViewModel_FormattedChangeStateToolTip_Added,
            FileChangeType.Modified => IsFile
                ? Language.IFileTreeNodeViewModel_FormattedChangeStateToolTip_ModifiedFile
                : Language.IFileTreeNodeViewModel_FormattedChangeStateToolTip_ModifiedFolder,
            FileChangeType.Removed => Language.IFileTreeNodeViewModel_FormattedChangeStateToolTip_Removed,
            FileChangeType.None => string.Empty,
        };
    }
    
}
