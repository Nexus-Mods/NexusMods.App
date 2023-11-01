using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;

/// <summary>
///     Represents an individual node in the 'Preview' section when selecting a location.
/// </summary>
[DebuggerDisplay("FileName = {FileName}, IsRoot = {IsRoot}, Children = {Children.Count}, Flags = {Flags}")]
public class TreeEntryViewModel : AViewModel<ITreeEntryViewModel>, ITreeEntryViewModel, IUnlinkableItem
{
    public TreeEntryViewModel? Parent { get; init; } = null!;

    // TODO: This (FullPath) should be optimized because we are creating a new string for every item.
    public GamePath FullPath { get; init; }

    public Subject<ITreeEntryViewModel> OnUnlinkRoot { get; } = new();
    [Reactive] public bool ShouldRemove { get; private set; } = false;
    public ObservableCollection<ITreeEntryViewModel> Children { get; init; } = new();
    public IUnlinkableItem? LinkedItem { get; private set; }

    public string FileName { get; init; }

    // Do not rearrange order here, flags are deliberately last to optimize for struct layout.
    public PreviewEntryNodeFlags Flags { get; private set; }

    // Derived Getters: For convenience.
    public string DirectoryName => FileName;
    public bool IsRoot => (Flags & PreviewEntryNodeFlags.IsRoot) == PreviewEntryNodeFlags.IsRoot;
    public bool IsDirectory => (Flags & PreviewEntryNodeFlags.IsDirectory) == PreviewEntryNodeFlags.IsDirectory;
    public bool IsNew => (Flags & PreviewEntryNodeFlags.IsNew) == PreviewEntryNodeFlags.IsNew;

    public bool IsFolderMerged =>
        (Flags & PreviewEntryNodeFlags.IsFolderMerged) == PreviewEntryNodeFlags.IsFolderMerged;

    public bool IsFolderDuplicated =>
        (Flags & PreviewEntryNodeFlags.IsFolderDuplicated) == PreviewEntryNodeFlags.IsFolderDuplicated;

    public TreeEntryViewModel(GamePath fullPath, PreviewEntryNodeFlags flags, TreeEntryViewModel? parent = null)
    {
        Parent = parent;
        FullPath = fullPath;
        Flags = flags;
        if (IsRoot)
            FileName = FullPath.LocationId.Value;
        else
            FileName = FullPath.FileName;

        this.WhenActivated(disposables =>
        {
            OnUnlinkRoot.DisposeWith(disposables);
        });
    }

    // Note: This is normally called from an 'unlinkable' item, i.e. ModContentNode
    public GamePath Bind(IUnlinkableItem unlinkable, DeploymentData data, bool previouslyExisted)
    {
        if (IsDirectory)
        {
            // Note: If two items merged into this item, then folders are considered 'merged'.
            if (previouslyExisted)
                Flags |= PreviewEntryNodeFlags.IsFolderMerged;
        }
        else
        {
            // Unlink previously bound item (if any).
            // set to true so it doesn't unlink itself
            LinkedItem?.Unlink(data, true);

            // We apply 'folder merged' flag under either of the circumstances.
            // 1. TODO: Files from two different subfolders are mapped to the same folder.
            //    - It's also unclear if 'folder merged' should be displayed when there are files
            //      from a subfolder where another bound file exists.
            //      i.e.
            //          - file
            //          - subfolder/file
            //      Should this display 'folder merged'?
            // 2. Folder already existed in game directory (non-empty), and we are mapping it.
            LinkedItem = unlinkable;
        }

        return FullPath;
    }

    public void Unlink(DeploymentData data, bool isCalledFromDoubleLinkedItem)
    {
        // Do the unlink.
        UnlinkRecursive(data, isCalledFromDoubleLinkedItem);

        // Delete self (if possible).
        if (!IsRoot)
        {
            // Remove self from parent.
            var thisVm = Parent!.Children.FirstOrDefault(x => x == this)!;
            Parent.Children.Remove(thisVm);

            var currentAncestor = Parent;

            // Remove all empty ancestors.
            while (currentAncestor?.Children.Count == 0)
            {
                // if the ancestor is root, we need to mark it for removal
                if (currentAncestor.IsRoot)
                {
                    Parent.ShouldRemove = true;
                    break;
                }

                // remove the ancestor from its parent
                var child = currentAncestor;
                currentAncestor = currentAncestor.Parent;

                var item = currentAncestor?.Children.FirstOrDefault(x => x == child);
                if (item != null)
                {
                    currentAncestor?.Children.Remove(item);
                }
            }
        }
        else
        {
            Children.Clear();
            ShouldRemove = true;
        }
    }

    private void UnlinkRecursive(DeploymentData data, bool isCalledFromDoubleLinkedItem)
    {
        // Recursively unlink first.
        foreach (var child in Children)
        {
            var node = child as TreeEntryViewModel;
            node!.UnlinkRecursive(data, isCalledFromDoubleLinkedItem);
        }

        // And now unlink self.
        if (!isCalledFromDoubleLinkedItem)
            LinkedItem?.Unlink(data, true);

        LinkedItem = null;
    }

    /// <summary>
    ///     Creates the root node for the preview tree from an existing path.
    /// </summary>
    /// <param name="fullPath">The path to create the node from.</param>
    /// <param name="isDirectory">True if the final part of the path is a directory.</param>
    /// <returns>The root node.</returns>
    public static TreeEntryViewModel Create(GamePath fullPath, bool isDirectory)
    {
        var root = new TreeEntryViewModel(new GamePath(fullPath.LocationId, ""),
            PreviewEntryNodeFlags.IsRoot | PreviewEntryNodeFlags.IsDirectory);

        // If there is no subpath, don't add any children.
        if (fullPath.Path.Path.Length == 0)
            return root;

        root.AddChildren(fullPath.Path, isDirectory, new AlwaysFalseChecker());
        return root;
    }

    /// <summary>
    ///     Adds a child node to the current node.
    /// </summary>
    /// <param name="relativePath">The path relative to current node.</param>
    /// <param name="isDirectory">True if the final part of the path is a directory.</param>
    /// <remarks>Adds a child to any non-root node.</remarks>
    public void AddChildren(string relativePath, bool isDirectory) =>
        AddChildren(relativePath, isDirectory, new AlwaysFalseChecker());

    private void AddChildren<TChecker>(string relativePath, bool isDirectory, TChecker checker)
        where TChecker : struct, ICheckIfItemAlreadyExists // for devirtualization, do not de-struct.
    {
        var pathComponents = relativePath.Split('/');
        var currentNode = this;

        for (var x = 0; x < pathComponents.Length; x++)
        {
            var component = pathComponents[x];
            var isLastComponent = x == pathComponents.Length - 1;

            // Check if the current node already has a child with the name of the current path component.
            var childNode = currentNode.Children.FirstOrDefault(child => child.FileName == component);

            // If the child node doesn't exist, create it.
            if (childNode == null)
            {
                var childFullPath = currentNode.FullPath.Path.Join(component);
                var newGamePath = new GamePath(FullPath.LocationId, childFullPath);

                var isNewFlag = checker.AlreadyExists(newGamePath)
                    ? PreviewEntryNodeFlags.IsNew
                    : PreviewEntryNodeFlags.Default;

                // All intermediate nodes are directories, last one depends on isDirectory.
                var isDirectoryFlag =  !isLastComponent || isDirectory
                    ? PreviewEntryNodeFlags.IsDirectory
                    : PreviewEntryNodeFlags.Default;

                childNode = new TreeEntryViewModel(newGamePath, isNewFlag | isDirectoryFlag, currentNode);
                currentNode.Children.Add(childNode);
            }

            // Set the current node to the child node and continue.
            currentNode = (TreeEntryViewModel)childNode;
        }
    }

    /// <summary>
    ///     Retrieves a child node from the current node using the relative path.
    /// </summary>
    /// <param name="relativePath">The path relative to current node.</param>
    /// <returns>The found node or null if not found.</returns>
    public ITreeEntryViewModel? GetChild(string relativePath)
    {
        if (relativePath == "")
            return this;

        var pathComponents = relativePath.Split('/');
        var currentNode = this;

        foreach (var component in pathComponents)
        {
            var childNode = currentNode.Children.FirstOrDefault(child => child.FileName == component);

            // If a child node with the given name is not found at any level, return null.
            if (childNode == null)
                return null;

            // Move to the next child node.
            currentNode = (TreeEntryViewModel)childNode;
        }

        return currentNode;
    }

    IModContentBindingTarget IModContentBindingTarget.GetOrCreateChild(string name, bool isDirectory)
    {
        var existing = GetChild(name);
        if (existing != null)
            return existing;

        AddChildren(name, isDirectory);
        return GetChild(name)!;
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
