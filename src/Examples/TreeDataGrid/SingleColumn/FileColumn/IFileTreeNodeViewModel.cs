using System.Runtime.CompilerServices;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI;
using NexusMods.App.UI.Helpers;

namespace Examples.TreeDataGrid.SingleColumn.FileColumn;

public interface IFileTreeNodeViewModel : IViewModelInterface, IExpandableItem
{
    /// <summary>
    ///     The icon that's used to display this specific node.
    /// </summary>
    FileTreeNodeIconType Icon { get; protected set; }
    
    /// <summary>
    ///     Name of the file or folder segment.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    ///     The key to this node.
    /// </summary>
    GamePath Key { get; }

    /// <summary>
    ///     The key to the parent of this node.
    /// </summary>
    GamePath ParentKey { get; }
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

public static class FileTreeNodeIconTypeExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetIconClass(this FileTreeNodeIconType iconType) => iconType switch
    {
        FileTreeNodeIconType.File => "File",
        FileTreeNodeIconType.ClosedFolder => "FolderOutline",
        FileTreeNodeIconType.OpenFolder => "FolderOpenOutline",
        _ => ThrowArgumentOutOfRangeException(iconType),
    };
    
    private static string ThrowArgumentOutOfRangeException(FileTreeNodeIconType iconType) => throw new ArgumentOutOfRangeException(nameof(iconType), iconType, null);
}

