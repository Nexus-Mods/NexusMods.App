using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NexusMods.Games.AdvancedInstaller.UI.Resources;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Left;

/// <summary>
///     Represents an individual node in a tree data grid for the Advanced Installer.
/// </summary>
/// <remarks>
///     Using this at runtime isn't exactly ideal given how many items there may be, but given everything is virtualized,
///     things should hopefully be a-ok!
/// </remarks>
public interface IModContentFileNode
{
    /// <summary>
    ///     Status of the node in question.
    /// </summary>
    public TreeDataGridSourceFileNodeStatus Status { get; }

    /// <summary>
    ///     The name of this specific file in the tree.
    /// </summary>
    string FileName { get; }

    /// <summary>
    ///     Contains the children nodes of this node.
    /// </summary>
    /// <remarks>
    ///     (Sewer) I got some notes to make here. Because someone will comment during review.
    ///     1.  The TreeDataGrid control does not dynamically update its children nodes (i.e. this list);
    ///     but instead consumes an IEnumerable. Therefore <see cref="ObservableCollection{T}" /> is not needed
    ///     (it would be unnecessary overhead).
    ///     2a. Although I wish it was possible, lazy loading this item is not really feasible.
    ///     When you map a folder, the state of all the children (recursively) must be updated;
    ///     meaning that the items (recursively) need to be loaded. Therefore, opportunities for lazy loading
    ///     are minimal.
    ///     2b. The input collection from which the tree is constructed is immutable. Mods cannot dynamically add
    ///     files in the middle of the Advanced Installer installation process.
    ///     Based on the above points, and given that the children count is already known in
    ///     <see cref="FileTreeNode{TPath,TValue}" />;
    ///     array is used, as it's the lowest overhead collection available for the job.
    /// </remarks>
    IModContentFileNode[] Children { get; }

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

/// <summary>
///     Represents a <see cref="IModContentFileNode" /> that is backed by a
///     <see cref="FileTreeNode{TPath,TValue}" />.
/// </summary>
/// <typeparam name="TRelPath">Type of relative path used in <see cref="FileTreeNode{TPath,TValue}" />.</typeparam>
/// <typeparam name="TNodeValue">Type of file entry used in <see cref="FileTreeNode{TPath,TValue}" />.</typeparam>
[DebuggerDisplay("FileName = {FileName}, IsRoot = {IsRoot}, Children = {Children.Length}, Status = {Status}")]
internal class ModContentNode<TRelPath, TNodeValue> : ReactiveObject, IModContentFileNode
    where TRelPath : struct, IPath<TRelPath>, IEquatable<TRelPath>
{
    /// <summary>
    ///     The underlying node providing the data for this tree.
    /// </summary>
    public required FileTreeNode<TRelPath, TNodeValue> Node { get; init; }

    /// <summary>
    ///     The parent of this node.
    /// </summary>
    /// <remarks>
    ///     This is null if the node is a root.
    /// </remarks>
    public required ModContentNode<TRelPath, TNodeValue>? Parent { get; init; }

    public required IModContentFileNode[] Children { get; init; }

    // Note: _lastStatus has no size impact on the object, because it fits in what otherwise would be padding.
    //       hence it was placed at the end of the object.
    [Reactive] public TreeDataGridSourceFileNodeStatus Status { get; private set; }
    private TreeDataGridSourceFileNodeStatus _lastStatus;

    public string FileName => Node.IsTreeRoot ? Language.FileTree_ALL_MOD_FILES : Node.Name;
    public bool IsDirectory => Node.IsDirectory;
    public bool IsRoot => Node.IsTreeRoot;

    /// <summary>
    /// Sets a new status, and stores the previous status in <see cref="_lastStatus" />.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStatus(TreeDataGridSourceFileNodeStatus status)
    {
        var last = Status;
        Status = status;
        _lastStatus = last;
    }

    /// <summary>
    /// Restores the last status backed up in <see cref="_lastStatus"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RestoreLastStatus() => (Status, _lastStatus) = (_lastStatus, Status);

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
        BeginSelectChildrenRecursive(this);
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
    public IEnumerable<ModContentNode<TRelPath, TNodeValue>> ChildrenFlattened()
    {
        var stack = new Stack<ModContentNode<TRelPath, TNodeValue>>();

        // Push initial children onto the stack.
        foreach (var child in Children)
            stack.Push((child as ModContentNode<TRelPath, TNodeValue>)!);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;
            foreach (var child in current.Children!)
                stack.Push((child as ModContentNode<TRelPath, TNodeValue>)!);
        }
    }

    /// <summary>
    ///     Recursively marks all of the children of this node for selection.
    /// </summary>
    public static void BeginSelectChildrenRecursive(ModContentNode<TRelPath, TNodeValue> item)
    {
        foreach (var childInterface in item.Children)
        {
            // Covariant cast to remove virtualization and make Status writeable.
            var child = childInterface as ModContentNode<TRelPath, TNodeValue>;
            child!.SetStatus(TreeDataGridSourceFileNodeStatus.SelectingViaParent);
            BeginSelectChildrenRecursive(child);
        }
    }

    /// <summary>
    ///     Recursively restores last status of all child nodes.
    /// </summary>
    public static void RestoreLastStatusRecursive(ModContentNode<TRelPath, TNodeValue> item)
    {
        foreach (var childInterface in item.Children)
        {
            // Covariant cast to remove virtualization and make Status writeable.
            var child = childInterface as ModContentNode<TRelPath, TNodeValue>;
            child!.RestoreLastStatus();
            RestoreLastStatusRecursive(child);
        }
    }

    /// <summary>
    ///     Creates a new <see cref="ModContentNode{TRelPath,TNodeValue}" /> from a given
    ///     <see cref="FileTreeNode{TRelPath,TFileEntry}" />.
    /// </summary>
    /// <typeparam name="TRelPath">Type of relative path used for the node.</typeparam>
    /// <typeparam name="TNodeValue">Type of value associated with this node.</typeparam>
    public static ModContentNode<TRelPath, TNodeValue> FromFileTree(FileTreeNode<TRelPath, TNodeValue> node)
    {
        var root = new ModContentNode<TRelPath, TNodeValue>
        {
            Node = node,
            Parent = null!,
            Children = GC.AllocateUninitializedArray<IModContentFileNode>(node.Children.Count),
            Status = TreeDataGridSourceFileNodeStatus.Default
        };

        var childIndex = 0;
        foreach (var child in node.Children)
            root.Children[childIndex++] = FromFileTreeRecursive(child.Value, root);

        return root;
    }

    /// <summary>
    ///     Recursively createsnew <see cref="ModContentNode{TRelPath,TNodeValue}" /> entries from a given matching
    ///     <see cref="FileTreeNode{TRelPath,TFileEntry}" /> node.
    /// </summary>
    /// <param name="node">The node of the file tree.</param>
    /// <param name="parent">The parent to the current entry.</param>
    /// <typeparam name="TRelPath">Type of relative path used for the node.</typeparam>
    /// <typeparam name="TNodeValue">Type of file entry stored in this tree.</typeparam>
    /// <returns>The node.</returns>
    public static IModContentFileNode FromFileTreeRecursive(FileTreeNode<TRelPath, TNodeValue> node,
        ModContentNode<TRelPath, TNodeValue> parent)
    {
        var item = new ModContentNode<TRelPath, TNodeValue>
        {
            Node = node,
            Parent = parent,
            Children = GC.AllocateUninitializedArray<IModContentFileNode>(node.Children.Count),
            Status = TreeDataGridSourceFileNodeStatus.Default
        };

        var childIndex = 0;
        foreach (var child in node.Children)
            item.Children[childIndex++] = FromFileTreeRecursive(child.Value, item);

        return item;
    }
}

/// <summary>
///     Represents the current status of the <see cref="ModContentNode{TRelPath,TNodeValue}" />.
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
