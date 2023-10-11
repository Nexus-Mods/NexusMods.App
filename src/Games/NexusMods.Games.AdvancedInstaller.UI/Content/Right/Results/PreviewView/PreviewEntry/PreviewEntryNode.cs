using NexusMods.Games.AdvancedInstaller.UI.Content.Left;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;

/// <summary>
///     Represents an individual node in the 'All Folders' section when selecting a location.
/// </summary>
/// <remarks>
///     We consider all entries delete-able, even those not added by the user in the results screen (such as
///     existing game folders that parents the selected mods).
///     This is such that the user can in one go delete all items as needed.
///     If it happens that after deletion, no files are deployed, the entire tree should be cleared.
/// </remarks>
public interface IPreviewEntryNode
{
    /// <summary>
    ///     Contains the children nodes of this node.
    /// </summary>
    /// <remarks>
    ///     See <see cref="ModContentNode{TRelPath,TNodeValue}.Children" />
    /// </remarks>
    IPreviewEntryNode[] Children { get; }

    /// <summary>
    ///     The file name displayed for this node.
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

    /*
       Note:
       In the case of folder, the item can be merged or created from multiple sources, e.g. Multiple folders.
       when we unlink the folder/node, the user expects that all of the items under this node are unlinked.

       Therefore, we need to maintain a list of all items we can run an 'unlink' operation on, which can be either
       a file or a directory.
    */

    /// <summary>
    ///     Collection of unlinkable items under this node.
    ///     This collection is null, unless an element exists.
    /// </summary>
    List<IUnlinkableItem>? UnlinkableItems { get; }
}

/// <summary>
///     Represents an individual node in the 'Preview' section when selecting a location.
/// </summary>
public class PreviewEntryNode : IPreviewEntryNode
{
    // TODO: Add this once we have concrete type for this.
    /// <summary>
    ///     The parent of this node.
    /// </summary>
    /// <remarks>
    ///     This is null if the node is a root.
    /// </remarks>
    // public required PreviewEntryNode<TRelPath, TNodeValue>? Parent { get; init; }

    public PreviewEntryNodeFlags Flags { get; init; }
    public IPreviewEntryNode[] Children { get; init; } = null!;
    public string FileName { get; init; } = null!;
    public List<IUnlinkableItem>? UnlinkableItems { get; } = new();

    // Note: Do not rearrange these fields (for packing/perf reasons).
    public bool IsRoot { get; init; }
    public bool IsDirectory { get; init; }
    public PreviewEntryNodeFlags DerivedFlags { get; init; }

    // Derived Getters (for convenience in ViewModel)

}

/// <summary>
///     Bitpacked flags describing the current state of the node.
/// </summary>
[Flags]
public enum PreviewEntryNodeFlags
{
    /// <summary>
    ///     If this is true, the 'new' pill should be displayed in the UI.
    /// </summary>
    IsNew = 0b0000_0001,

    /// <summary>
    ///     If this is true the 'folder merged' pill should be displayed in the UI.
    /// </summary>
    IsFolderMerged = 0b0000_0010
}
