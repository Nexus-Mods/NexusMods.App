using System.Collections.ObjectModel;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

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
public interface IPreviewEntryNode : IModContentBindingTarget
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
    ObservableCollection<ITreeEntryViewModel> Children { get; }

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

    /// <summary>
    ///     If this is true the 'dupe folder' pill should be displayed in the UI.
    /// </summary>
    bool IsFolderDuplicated { get; }

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
    // TODO: This (FullPath) should be optimized because we are creating a new string for every item.
    public GamePath FullPath { get; init; }
    public ObservableCollection<ITreeEntryViewModel> Children { get; init; } = new();
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

    public bool IsFolderDuplicated =>
        (Flags & PreviewEntryNodeFlags.IsFolderDuplicated) == PreviewEntryNodeFlags.IsFolderDuplicated;


    public PreviewEntryNode(GamePath fullPath, PreviewEntryNodeFlags flags)
    {
        FullPath = fullPath;
        Flags = flags;
    }

    // Note: This is normally called
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

    /// <summary>
    ///     Creates the root node for the preview tree from an existing path.
    /// </summary>
    /// <param name="fullPath">The path to create the node from.</param>
    /// <param name="isDirectory">True if the final part of the path is a directory.</param>
    /// <returns>The root node.</returns>
    public static PreviewEntryNode Create(GamePath fullPath, bool isDirectory)
    {
        var root = new PreviewEntryNode(new GamePath(fullPath.LocationId, ""),
            PreviewEntryNodeFlags.IsRoot | PreviewEntryNodeFlags.IsDirectory);
        root.AddChild(fullPath.Path, isDirectory, new AlwaysFalseChecker());
        return root;
    }

    /// <summary>
    ///     Adds a child node to the current node.
    /// </summary>
    /// <param name="relativePath">The path relative to current node.</param>
    /// <param name="isDirectory">True if the final part of the path is a directory.</param>
    /// <remarks>Adds a child to any non-root node.</remarks>
    public void AddChild(string relativePath, bool isDirectory) =>
        AddChild(relativePath, isDirectory, new AlwaysFalseChecker());

    private void AddChild<TChecker>(string relativePath, bool isDirectory, TChecker checker)
        where TChecker : struct, ICheckIfItemAlreadyExists // for devirtualization, do not de-struct.
    {
        var pathComponents = relativePath.Split('/');
        var currentNode = this;

        for (var x = 0; x < pathComponents.Length; x++)
        {
            var component = pathComponents[x];
            var isLastComponent = x == pathComponents.Length - 1;

            // Check if the current node already has a child with the name of the current path component.
            var childNode = currentNode.Children.FirstOrDefault(child => child.Node.AsT2.FileName == component);

            // If the child node doesn't exist, create it.
            if (childNode == null)
            {
                var childFullPath = currentNode.FullPath.Path.Join(component);
                var newGamePath = new GamePath(FullPath.LocationId, childFullPath);

                var isNewFlag = checker.AlreadyExists(newGamePath)
                    ? PreviewEntryNodeFlags.IsNew
                    : PreviewEntryNodeFlags.Default;

                var isDirectoryFlag = isLastComponent && !isDirectory
                    ? PreviewEntryNodeFlags.Default
                    : PreviewEntryNodeFlags.IsDirectory;

                childNode = new TreeEntryViewModel(new PreviewEntryNode(newGamePath, isNewFlag | isDirectoryFlag));
                currentNode.Children.Add(childNode);
            }

            // Set the current node to the child node and continue.
            currentNode = (PreviewEntryNode)childNode.Node.AsT2;
        }
    }

    /// <summary>
    ///     Retrieves a child node from the current node using the relative path.
    /// </summary>
    /// <param name="relativePath">The path relative to current node.</param>
    /// <returns>The found node or null if not found.</returns>
    public IPreviewEntryNode? GetChild(string relativePath)
    {
        var pathComponents = relativePath.Split('/');
        var currentNode = this;

        foreach (var component in pathComponents)
        {
            var childNode = currentNode.Children.FirstOrDefault(child => child.FileName == component);

            // If a child node with the given name is not found at any level, return null.
            if (childNode == null)
                return null;

            // Move to the next child node.
            currentNode = (PreviewEntryNode)childNode;
        }

        return currentNode;
    }
}

/// <summary>
///     Bitpacked flags describing the current state of the node.
/// </summary>
[Flags]
public enum PreviewEntryNodeFlags
{
    /// <summary>
    ///     None
    /// </summary>
    Default,

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
    IsRoot = 0b0000_1000,

    /// <summary>
    ///     If this is true, this folder has the same name as its parent.
    /// </summary>
    IsFolderDuplicated = 0b0001_0000,
}

/// <summary>
///     An interface that informs the node adding process whether an item has previously existed.
/// </summary>
public interface ICheckIfItemAlreadyExists
{
    /// <summary>
    ///     Returns true if the given path already exist
    /// </summary>
    /// <param name="path">The path to validate if it already exists in the game folder.</param>
    /// <returns>True if this path already exists, else false.</returns>
    bool AlreadyExists(GamePath path);
}

/// <summary>
///     A checker that always returns false.
/// </summary>
internal struct AlwaysFalseChecker : ICheckIfItemAlreadyExists
{
    public bool AlreadyExists(GamePath path) => true;
}
