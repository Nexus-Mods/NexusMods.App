using System.Diagnostics;
using System.Runtime.CompilerServices;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;
using NexusMods.Games.AdvancedInstaller.UI.Resources;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Left;

/// <summary>
///     Represents an individual node in the 'Mod Content' section.
///     A node can represent any file or directory within the mod being unpacked during advanced install.
/// </summary>
/// <remarks>
///     Using this at runtime isn't exactly ideal given how many items there may be, but given everything is virtualized,
///     things should hopefully be a-ok!
/// </remarks>
public interface IModContentNode : IUnlinkableItem
{
    /// <summary>
    ///     Status of the node in question.
    /// </summary>
    [Reactive] public TreeDataGridSourceFileNodeStatus Status { get; }

    /// <summary>
    ///     True if this is an element child of the root node.
    /// </summary>
    /// <remarks>
    ///     This is useful for the UI, e.g. to determine "Included" vs "Included with folder" text.
    /// </remarks>
    bool IsTopLevel { get; }

    /// <summary>
    ///     The name of this specific file in the tree.
    /// </summary>
    string FileName { get; }

    /// <summary>
    ///     Name of the linked target which was created with <see cref="Link"/>.
    /// </summary>
    /// <remarks>
    ///     This is used such that we can unlink the entry on the left hand side.
    /// </remarks>
    IModContentBindingTarget? LinkedTarget { get; }

    /// <summary>
    ///     Contains the children nodes of this node.
    /// </summary>
    /// <remarks>
    ///     (Sewer) I got some notes to make here.
    ///
    ///     1. Lazy loading of this item should be investigated, in the case that the user has not yet expanded all
    ///        items yet.
    ///
    ///
    ///        When you map a folder, the state of all the children (recursively) must be updated;
    ///        meaning that the items (recursively) need to be loaded. Therefore, opportunities for lazy loading
    ///        are minimal.
    ///
    ///     2. The input collection from which the tree is constructed is immutable.
    ///
    ///        Mods cannot dynamically add files in the middle of the Advanced Installer
    ///        installation process. There is no need to use an observable collection here,
    ///        as that would just be unnecessary memory overhead.
    ///
    ///     Based on the above points, and given that the children count is already known in
    ///     <see cref="FileTreeNode{TPath,TValue}" />; an array is used, as it's the lowest
    ///     overhead collection available for the job.
    /// </remarks>
    ITreeEntryViewModel[] Children { get; }

    /// <summary>
    ///     True if this is the root node.
    /// </summary>
    bool IsRoot { get; }

    /// <summary>
    ///     True if this is a directory, in which case all files from child of this will be mapped to given
    ///     target folder.
    /// </summary>
    new bool IsDirectory { get; }

    /// <summary>
    ///     Binds the current node/source to the given target.
    /// </summary>
    /// <param name="data">The structure keeping track of deployment data.</param>
    /// <param name="target">The target to receive the binding.</param>
    void Link(DeploymentData data, IModContentBindingTarget target);
}

/// <summary>
///     Represents an item that can be unlinked from the deployment data.
/// </summary>
public interface IUnlinkableItem
{
    /// <summary>
    ///     Returns true if this unlinkable item represents a folder, else false.
    /// </summary>
    public bool IsDirectory { get; }

    /// <summary>
    ///     Unlink the current node from the deployment data.
    /// </summary>
    public void Unlink(DeploymentData data);
}

/// <summary>
///     Represents a <see cref="IModContentNode" /> that is backed by a
///     <see cref="FileTreeNode{TPath,TValue}" />.
/// </summary>
/// <typeparam name="TNodeValue">Type of file entry used in <see cref="FileTreeNode{TPath,TValue}" />.</typeparam>
[DebuggerDisplay("FileName = {FileName}, IsRoot = {IsRoot}, Children = {Children.Length}, Status = {Status}")]
internal class ModContentNode<TNodeValue> : ReactiveObject, IModContentNode
{
    private TreeDataGridSourceFileNodeStatus _lastStatus;

    /// <summary>
    ///     The underlying node providing the data for this tree.
    /// </summary>
    public required FileTreeNode<RelativePath, TNodeValue> Node { get; init; }

    /// <summary>
    ///     The parent of this node.
    /// </summary>
    /// <remarks>
    ///     This is null if the node is a root.
    /// </remarks>
    public required ModContentNode<TNodeValue>? Parent { get; init; }

    /// <inheritdoc />
    public IModContentBindingTarget? LinkedTarget { get; private set; }

    /// <inheritdoc />
    public required ITreeEntryViewModel[] Children { get; init; }

    // Note: _lastStatus has no size impact on the object, because it fits in what otherwise would be padding.
    //       hence it was placed at the end of the object.
    [Reactive] public TreeDataGridSourceFileNodeStatus Status { get; private set; }

    /// <summary>
    ///     Whether the node is a child of the root.
    /// </summary>
    public required bool IsTopLevel { get; init; }

    public string FileName => Node.IsTreeRoot ? Language.FileTree_ALL_MOD_FILES : Node.Name;
    public bool IsDirectory => Node.IsDirectory;
    public bool IsRoot => Node.IsTreeRoot;

    public void Link(DeploymentData data, IModContentBindingTarget target)
    {
        // TODO: Handle individual file nodes.
        LinkedTarget = target;
        data.AddFolderMapping(Node, target.Bind(this));
        SetStatusRecursive(this, TreeDataGridSourceFileNodeStatus.IncludedViaParent);
    }

    public void Unlink(DeploymentData data)
    {
        data.RemoveFolderMapping(Node);
    }

    /// <summary>
    ///     Sets a new status, and stores the previous status in <see cref="_lastStatus" />.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStatus(TreeDataGridSourceFileNodeStatus status)
    {
        var last = Status;
        Status = status;
        _lastStatus = last;
    }

    /// <summary>
    ///     Restores the last status backed up in <see cref="_lastStatus" />
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RestoreLastStatus()
    {
        (Status, _lastStatus) = (_lastStatus, Status);
    }

    /// <summary>
    ///     Marks the node for selection, changing its state to <see cref="TreeDataGridSourceFileNodeStatus.Selecting" />,
    ///     and updating the state of the child nodes accordingly.
    /// </summary>
    public void BeginSelect()
    {
        SetStatus(TreeDataGridSourceFileNodeStatus.Selecting);
        if (!IsDirectory)
            return;

        // Update all of children
        SetStatusRecursive(this, TreeDataGridSourceFileNodeStatus.Selecting);
    }

    /// <summary>
    ///     Cancels the selection operation on the current node.
    /// </summary>
    public void CancelSelect()
    {
        if (Status != TreeDataGridSourceFileNodeStatus.Selecting)
            return;

        RestoreLastStatusRecursive();
    }

    /// <summary>
    ///     Recursively restores the last status of all of the nodes.
    /// </summary>
    public void RestoreLastStatusRecursive()
    {
        RestoreLastStatus();
        if (!IsDirectory)
            return;

        RestoreLastStatusRecursive(this);
    }

    /// <summary>
    ///     Enumerates all children of this node, in a flattened fashion, using a depth first search approach.
    /// </summary>
    /// <remarks>
    ///     Uses stack to avoid recursive IEnumerable, which would be a performance disaster.
    /// </remarks>
    public IEnumerable<ModContentNode<TNodeValue>> ChildrenFlattened()
    {
        var stack = new Stack<ModContentNode<TNodeValue>>();

        // Push initial children onto the stack.
        foreach (var child in Children)
            stack.Push((child as ModContentNode<TNodeValue>)!);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;
            foreach (var child in current.Children)
                stack.Push((child as ModContentNode<TNodeValue>)!);
        }
    }

    /// <summary>
    ///     Recursively sets a new status for every single node under this node.
    /// </summary>
    private static void SetStatusRecursive(ModContentNode<TNodeValue> item, TreeDataGridSourceFileNodeStatus status)
    {
        foreach (var childInterface in item.Children)
        {
            // Covariant cast to remove virtualization and make Status writeable.
            var child = childInterface as ModContentNode<TNodeValue>;
            child!.SetStatus(status);
            SetStatusRecursive(child, status);
        }
    }

    /// <summary>
    ///     Recursively restores last status of all child nodes.
    /// </summary>
    private static void RestoreLastStatusRecursive(ModContentNode<TNodeValue> item)
    {
        foreach (var childInterface in item.Children)
        {
            // Covariant cast to remove virtualization and make Status writeable.
            var child = childInterface as ModContentNode<TNodeValue>;
            child!.RestoreLastStatus();
            RestoreLastStatusRecursive(child);
        }
    }

    /// <summary>
    ///     Creates a new <see cref="ModContentNode{TNodeValue}" /> from a given
    ///     <see cref="FileTreeNode{RelativePath,TFileEntry}" />. The entry is assumed to be
    ///     the root.
    /// </summary>
    /// <param name="node">The root node.</param>
    /// <typeparam name="TNodeValue">Type of value associated with this node.</typeparam>
    public static ModContentNode<TNodeValue> FromFileTree(FileTreeNode<RelativePath, TNodeValue> node)
    {
        var root = new ModContentNode<TNodeValue>
        {
            Node = node,
            Parent = null!,
            Children = GC.AllocateUninitializedArray<ITreeEntryViewModel>(node.Children.Count),
            IsTopLevel = false,
            Status = TreeDataGridSourceFileNodeStatus.Default
        };

        var childIndex = 0;
        foreach (var child in node.Children)
            root.Children[childIndex++] = new TreeEntryViewModel(FromFileTreeRecursive(child.Value, root));

        return root;
    }

    /// <summary>
    ///     Recursively creates new <see cref="ModContentNode{TNodeValue}" /> entries from a given matching
    ///     <see cref="FileTreeNode{RelativePath,TFileEntry}" /> node.
    /// </summary>
    /// <param name="node">The node of the file tree.</param>
    /// <param name="parent">The parent to the current entry.</param>
    /// <typeparam name="TNodeValue">Type of file entry stored in this tree.</typeparam>
    /// <returns>The node.</returns>
    public static IModContentNode FromFileTreeRecursive(FileTreeNode<RelativePath, TNodeValue> node,
        ModContentNode<TNodeValue> parent)
    {
        var item = new ModContentNode<TNodeValue>
        {
            Node = node,
            Parent = parent,
            Children = GC.AllocateUninitializedArray<ITreeEntryViewModel>(node.Children.Count),
            IsTopLevel = parent.IsRoot,
            Status = TreeDataGridSourceFileNodeStatus.Default
        };

        var childIndex = 0;
        foreach (var child in node.Children)
            item.Children[childIndex++] = new TreeEntryViewModel(FromFileTreeRecursive(child.Value, item));

        return item;
    }
}

/// <summary>
///     Represents the current status of the <see cref="ModContentNode{TNodeValue}" />.
/// </summary>
public enum TreeDataGridSourceFileNodeStatus
{
    /// <summary>
    ///     Item is not selected, and available for selection.
    /// </summary>
    Default,

    /// <summary>
    ///     The item target is currently being selected/mapped.
    ///     This is used by the item which is currently being mapped into an install location.
    /// </summary>
    Selecting,

    /// <summary>
    ///     A parent of this item (folder) is currently being selected/mapped.
    /// </summary>
    /// <remarks>
    ///     When this state is active, the UI shows 'include' for files, and 'include folder' for folders.
    /// </remarks>
    SelectingViaParent,

    /// <summary>
    ///     Item is included, with explicit target location.
    /// </summary>
    /// <remarks>
    ///     When this state is active, the UI usually shows the name of the linked folder in the associated button.
    /// </remarks>
    IncludedExplicit,

    /// <summary>
    ///     Item id included, because a parent (folder) of the item is included.
    ///     When the parent is unlinked, this node is also unlinked.
    /// </summary>
    /// <remarks>
    ///     This is used to indicate a parent of this item which which is a directory has status
    ///     <see cref="IncludedExplicit" />.
    /// </remarks>
    IncludedViaParent
}
