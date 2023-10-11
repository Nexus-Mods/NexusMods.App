using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using static NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry.PreviewEntryNodeFlags;

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
    ///     See <see cref="ModContentNode{TNodeValue}.Children" />
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

    /// <summary>
    ///     Applies a link from source to the given node.
    /// </summary>
    /// <param name="source">The source item that was linked to this node.</param>
    /// <param name="previouslyExisted">True if this item has previously existed in the game directory.</param>
    void ApplyLink(IUnlinkableItem source, bool previouslyExisted);
}

/// <summary>
///     Represents an individual node in the 'Preview' section when selecting a location.
/// </summary>
public class PreviewEntryNode : IPreviewEntryNode
{
    public PreviewEntryNodeFlags Flags { get; init; }

    public PreviewEntryNodeFlags DerivedFlags { get; init; }

    public IPreviewEntryNode[] Children { get; init; } = null!;
    public string FileName { get; init; } = null!;
    public List<IUnlinkableItem>? UnlinkableItems { get; } = new();

    public void ApplyLink(IUnlinkableItem source, bool previouslyExisted)
    {
        // We apply 'folder merged' flag under either of the circumstances.
        // 1. Files from two different subfolders are mapped to the same folder.
        // 2. Folder already existed in game directory (non-empty), and we are mapping it.
        throw new NotImplementedException();
    }

    // Note: Do not rearrange these fields (for packing/perf reasons).
    public bool IsRoot { get; init; }
    public bool IsDirectory { get; init; }

    // Derived Getters (for convenience in ViewModel)
    public bool IsNew => (Flags & PreviewEntryNodeFlags.IsNew) == PreviewEntryNodeFlags.IsNew;

    public bool IsFolderMerged =>
        (Flags & PreviewEntryNodeFlags.IsFolderMerged) == PreviewEntryNodeFlags.IsFolderMerged;
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
