using System.Diagnostics;
using System.Runtime.CompilerServices;
using NexusMods.Games.AdvancedInstaller.UI.Resources;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Left;

/// <summary>
///     Represents a <see cref="ITreeEntryViewModel" /> that is backed by a
///     <see cref="FileTreeNode{TPath,TValue}" />.
/// </summary>
/// <typeparam name="TNodeValue">Type of file entry used in <see cref="FileTreeNode{TPath,TValue}" />.</typeparam>
[DebuggerDisplay("FileName = {FileName}, IsRoot = {IsRoot}, Children = {Children.Length}, Status = {Status}")]
internal class TreeEntryViewModel<TNodeValue> : ReactiveObject, ITreeEntryViewModel
{
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
    public required TreeEntryViewModel<TNodeValue>? Parent { get; init; }

    /// <inheritdoc />
    public IModContentBindingTarget? LinkedTarget { get; private set; }

    /// <inheritdoc />
    public required ITreeEntryViewModel[] Children { get; init; }

    // Note: Items here are reduced to 1 byte, to avoid eating memory. With 3 items we have 5 bytes of padding left.
    [Reactive] public ModContentNodeStatus Status { get; private set; }
    private ModContentNodeStatus _lastStatus;

    /// <summary>
    ///     Whether the node is a child of the root.
    /// </summary>
    public required bool IsTopLevel { get; init; }

    public string FileName => Node.IsTreeRoot ? Language.FileTree_ALL_MOD_FILES : Node.Name;
    public RelativePath FullPath => Node.Path;
    public bool IsDirectory => Node.IsDirectory;
    public bool IsRoot => Node.IsTreeRoot;

    public void Link(DeploymentData data, IModContentBindingTarget target, bool targetAlreadyExisted)
    {
        LinkedTarget = target;
        SetStatus(ModContentNodeStatus.IncludedExplicit);

        if (!IsDirectory)
        {
            var folder = target.Bind(this, targetAlreadyExisted);
            data.AddMapping(Node.Path, new GamePath(folder.LocationId, folder.Path.Join(FileName)), true);
            return;
        }

        foreach (var child in Children)
        {
            var node = child as TreeEntryViewModel<TNodeValue>;
            LinkRecursive(node!, data, target.GetOrCreateChild(node!.FileName, node.IsDirectory), targetAlreadyExisted);
        }
    }

    private static void LinkRecursive(TreeEntryViewModel<TNodeValue> @this, DeploymentData data,
        IModContentBindingTarget target,
        bool targetAlreadyExisted)
    {
        @this.SetStatus(ModContentNodeStatus.IncludedViaParent);
        if (@this.IsDirectory)
        {
            foreach (var child in @this.Children)
            {
                var node = child as TreeEntryViewModel<TNodeValue>;
                LinkRecursive(node!, data, target.GetOrCreateChild(node!.FileName, node.IsDirectory),
                    targetAlreadyExisted);
            }
        }
        else
        {
            var filePath = target.Bind(@this, targetAlreadyExisted);
            data.AddMapping(@this.Node.Path, new GamePath(filePath.LocationId, filePath.Path),
                true);
        }
    }

    public void Unlink(DeploymentData data)
    {
        SetStatus(ModContentNodeStatus.Default);

        if (IsDirectory)
        {
            SetStatus(ModContentNodeStatus.Default);
            foreach (var child in Children)
            {
                var node = child as TreeEntryViewModel<TNodeValue>;
                node!.Unlink(data);
            }
        }
        else
        {
            data.RemoveMapping(Node.Path);
        }
    }

    /// <summary>
    ///     Sets a new status, and stores the previous status in <see cref="_lastStatus" />.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStatus(ModContentNodeStatus status)
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
    ///     Marks the node for selection, changing its state to <see cref="ModContentNodeStatus.Selecting" />,
    ///     and updating the state of the child nodes accordingly.
    /// </summary>
    public void BeginSelect()
    {
        SetStatus(ModContentNodeStatus.Selecting);
        if (!IsDirectory)
            return;

        // Update all of children
        SetStatusRecursive(this, ModContentNodeStatus.SelectingViaParent);
    }

    /// <summary>
    ///     Cancels the selection operation on the current node.
    /// </summary>
    public void CancelSelect()
    {
        if (Status != ModContentNodeStatus.Selecting)
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
    public IEnumerable<TreeEntryViewModel<TNodeValue>> ChildrenFlattened()
    {
        var stack = new Stack<TreeEntryViewModel<TNodeValue>>();

        // Push initial children onto the stack.
        foreach (var child in Children)
            stack.Push((child as TreeEntryViewModel<TNodeValue>)!);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;
            foreach (var child in current.Children)
                stack.Push((child as TreeEntryViewModel<TNodeValue>)!);
        }
    }

    /// <summary>
    ///     Recursively sets a new status for every single node under this node.
    /// </summary>
    private static void SetStatusRecursive(TreeEntryViewModel<TNodeValue> item, ModContentNodeStatus status)
    {
        foreach (var childInterface in item.Children)
        {
            // Covariant cast to remove virtualization and make Status writeable.
            var child = childInterface as TreeEntryViewModel<TNodeValue>;
            child!.SetStatus(status);
            SetStatusRecursive(child, status);
        }
    }

    /// <summary>
    ///     Recursively restores last status of all child nodes.
    /// </summary>
    private static void RestoreLastStatusRecursive(TreeEntryViewModel<TNodeValue> item)
    {
        foreach (var childInterface in item.Children)
        {
            // Covariant cast to remove virtualization and make Status writeable.
            var child = childInterface as TreeEntryViewModel<TNodeValue>;
            child!.RestoreLastStatus();
            RestoreLastStatusRecursive(child);
        }
    }

    /// <summary>
    ///     Creates a new <see cref="TreeEntryViewModel{TNodeValue}" /> from a given
    ///     <see cref="FileTreeNode{RelativePath,TFileEntry}" />. The entry is assumed to be
    ///     the root.
    /// </summary>
    /// <param name="node">The root node.</param>
    /// <typeparam name="TNodeValue">Type of value associated with this node.</typeparam>
    public static TreeEntryViewModel<TNodeValue> FromFileTree(FileTreeNode<RelativePath, TNodeValue> node)
    {
        var root = new TreeEntryViewModel<TNodeValue>
        {
            Node = node,
            Parent = null!,
            Children = GC.AllocateUninitializedArray<ITreeEntryViewModel>(node.Children.Count),
            IsTopLevel = false,
            Status = ModContentNodeStatus.Default
        };

        var childIndex = 0;
        foreach (var child in node.Children)
            root.Children[childIndex++] = FromFileTreeRecursive(child.Value, root);

        return root;
    }

    /// <summary>
    ///     Recursively creates new <see cref="TreeEntryViewModel{TNodeValue}" /> entries from a given matching
    ///     <see cref="FileTreeNode{RelativePath,TFileEntry}" /> node.
    /// </summary>
    /// <param name="node">The node of the file tree.</param>
    /// <param name="parent">The parent to the current entry.</param>
    /// <typeparam name="TNodeValue">Type of file entry stored in this tree.</typeparam>
    /// <returns>The node.</returns>
    public static ITreeEntryViewModel FromFileTreeRecursive(FileTreeNode<RelativePath, TNodeValue> node,
        TreeEntryViewModel<TNodeValue> parent)
    {
        var item = new TreeEntryViewModel<TNodeValue>
        {
            Node = node,
            Parent = parent,
            Children = GC.AllocateUninitializedArray<ITreeEntryViewModel>(node.Children.Count),
            IsTopLevel = parent.IsRoot,
            Status = ModContentNodeStatus.Default
        };

        var childIndex = 0;
        foreach (var child in node.Children)
            item.Children[childIndex++] = FromFileTreeRecursive(child.Value, item);

        return item;
    }
}

/// <summary>
///     Represents the current status of the <see cref="TreeEntryViewModel{TNodeValue}" />.
/// </summary>
public enum ModContentNodeStatus : byte
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
