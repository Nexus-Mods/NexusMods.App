using NexusMods.Abstractions.GameLocators;

namespace NexusMods.App.UI.Controls.Trees.Files;

public interface IFileTreeNodeViewModel : IViewModelInterface
{
    /// <summary>
    ///     The icon that's used to display this specific node.
    /// </summary>
    FileTreeNodeIconType Icon { get; protected set; }
    
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
    ///     The key to this node.
    /// </summary>
    object Key { get; }

    /// <summary>
    ///     The key to the parent of this node.
    /// </summary>
    object ParentKey { get; }

    /// <summary>
    ///     A method called when the tree item is expanded, this by default changes the icon style.
    /// </summary>
    /// <param name="isExpanded">True if the item is expanded.</param>
    void OnExpanded(bool isExpanded)
    {
        if (IsFile)
            return;

        Icon = isExpanded ? FileTreeNodeIconType.OpenFolder : FileTreeNodeIconType.ClosedFolder;
    }
}

public enum FileTreeNodeIconType
{
    /// <summary>
    ///     Shows a regular 'file' icon.
    /// </summary>
    File,
    
    /// <summary>
    ///     Show a 'closed folder' icon.
    /// </summary>
    ClosedFolder,
    
    /// <summary>
    ///     Show an 'open folder' icon.
    /// </summary>
    OpenFolder,
}
