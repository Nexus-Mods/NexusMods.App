using NexusMods.Games.AdvancedInstaller.UI.Content.Left;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;

/// <summary>
///     Represents an individual node in the 'All Folders' section when selecting a location.
/// </summary>
/// <remarks>
///     We consider all entries delete-able, even those not added by the user in the results screen (such as
///     existing game folders that parents the selected mods).
///
///     This is such that the user can in one go delete all items as needed.
///
///     If it happens that after deletion, no files are deployed, the entire tree should be cleared.
/// </remarks>
public interface IPreviewEntryNode
{
    /// <summary>
    ///     Contains the children nodes of this node.
    /// </summary>
    /// <remarks>
    ///     See <see cref="ModContentNode{TRelPath,TNodeValue}.Children"/>
    /// </remarks>
    IPreviewEntryNode[] Children { get; }

    /// <summary>
    /// The file name displayed for this node.
    /// </summary>
    string FileName { get; }

    /// <summary>
    ///     True if this is the root node.
    /// </summary>
    bool IsRoot { get; }

    /// <summary>
    ///     True if this is a directory, in which case all files from child of this will be mapped to given
    ///     target folder.
    /// </summary>
    bool IsDirectory { get; }
}

public class PreviewEntryNode : IPreviewEntryNode
{
    // TODO: Add this once we have concrete type.
    /// <summary>
    ///     The parent of this node.
    /// </summary>
    /// <remarks>
    ///     This is null if the node is a root.
    /// </remarks>
    // public required PreviewEntryNode<TRelPath, TNodeValue>? Parent { get; init; }

    public IPreviewEntryNode[] Children { get; init; } = null!;

    public string FileName { get; init; } = null!;
    public bool IsRoot { get; init; }
    public bool IsDirectory { get; init; }
}

public enum PreviewEntryNodeType { }
