using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;
using NexusMods.Paths;

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
    ///     The full path of this node.
    /// </summary>
    public GamePath FullPath { get; init; }

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
    ///     True if this is the root node. (Cannot be deleted)
    /// </summary>
    bool IsRoot { get; }

    /// <summary>
    ///     True if this is a directory, in which case all files from child of this will be mapped to given
    ///     target folder.
    /// </summary>
    bool IsDirectory { get; }

    /// <summary>
    ///     If this is true, the 'new' pill should be displayed in the UI.
    /// </summary>
    bool IsNew { get; }

    /// <summary>
    ///     If this is true the 'folder merged' pill should be displayed in the UI.
    /// </summary>
    bool IsFolderMerged { get; }

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
public class PreviewEntryNode : IPreviewEntryNode, IModContentBindingTarget
{
    public GamePath FullPath { get; init; }
    public IPreviewEntryNode[] Children { get; init; } = null!;
    public List<IUnlinkableItem>? UnlinkableItems { get; private set; } = new();

    // Do not rearrange order here, flags are deliberately last to optimize for struct layout.
    public PreviewEntryNodeFlags Flags { get; private set; }

    // Derived Getters: For convenience.
    public string FileName => FullPath.FileName;
    public string DirectoryName => FileName;
    public bool IsRoot => (Flags & PreviewEntryNodeFlags.IsRoot) == PreviewEntryNodeFlags.IsRoot;
    public bool IsDirectory => (Flags & PreviewEntryNodeFlags.IsDirectory) == PreviewEntryNodeFlags.IsDirectory;
    public bool IsNew => (Flags & PreviewEntryNodeFlags.IsNew) == PreviewEntryNodeFlags.IsNew;

    public bool IsFolderMerged =>
        (Flags & PreviewEntryNodeFlags.IsFolderMerged) == PreviewEntryNodeFlags.IsFolderMerged;

    public GamePath Bind(IUnlinkableItem unlinkable, bool previouslyExisted)
    {
        // We apply 'folder merged' flag under either of the circumstances.
        // 1. TODO: Files from two different subfolders are mapped to the same folder.
        //    - It's also unclear if 'folder merged' should be displayed when there are files
        //      from a subfolder where another bound file exists.
        //      i.e.
        //          - file
        //          - subfolder/file
        //      Should this display 'folder merged'?
        // 2. Folder already existed in game directory (non-empty), and we are mapping it.
        UnlinkableItems ??= new List<IUnlinkableItem>();
        UnlinkableItems.Add(unlinkable);

        // Note: If two items merged into this item, then folders are considered 'merged'.
        if (previouslyExisted)
            Flags |= PreviewEntryNodeFlags.IsFolderMerged;

        return FullPath;
    }

    /// <summary>
    ///     Unlinks this node, and all children (in the case this node is a telegram).
    /// </summary>
    public void Unlink(DeploymentData data)
    {
        if (UnlinkableItems == null)
            return;

        foreach (var unlinkable in UnlinkableItems)
            unlinkable.Unlink(data);

        UnlinkableItems.Clear();
    }
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
    IsFolderMerged = 0b0000_0010,

    /// <summary>
    ///     If this is true, this item is a directory.
    /// </summary>
    IsDirectory = 0b0000_0100,

    /// <summary>
    ///     If this is true, this node is the root, and cannot be deleted.
    ///     (All items can however be unlinked)
    /// </summary>
    IsRoot = 0b0000_1000
}
