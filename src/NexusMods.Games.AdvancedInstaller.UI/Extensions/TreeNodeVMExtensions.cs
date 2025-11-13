using System.Collections.ObjectModel;
using DynamicData.Kernel;

using NexusMods.Games.AdvancedInstaller.UI.ModContent;
using NexusMods.Games.AdvancedInstaller.UI.Preview;
using NexusMods.Games.AdvancedInstaller.UI.SelectLocation;
using NexusMods.Paths;
using NexusMods.Sdk.Games;

namespace NexusMods.Games.AdvancedInstaller.UI;

using ModContentNode = TreeNodeVM<IModContentTreeEntryViewModel, RelativePath>;
using PreviewNode = TreeNodeVM<IPreviewTreeEntryViewModel, GamePath>;
using SelectableNode = TreeNodeVM<ISelectableTreeEntryViewModel, GamePath>;

/// <summary>
/// Some extension methods for <see cref="TreeNodeVM{TEntry, TPath}"/> and <see cref="ReadOnlyObservableCollection{T}"/>.
/// In particular to help with finding nodes in the tree or in a collection of roots.
/// </summary>
public static class TreeNodeVMExtensions
{
    /// <summary>
    /// Attempts to find a descendent node given it's <paramref name="path"/>.
    /// </summary>
    /// <param name="node">The current node</param>
    /// <param name="path">The path (relative to the tree root) of the node to find.</param>
    /// <returns>An optional containing the value if found.</returns>
    public static Optional<ModContentNode> GetTreeNode(this ModContentNode node, RelativePath path)
    {
        if (node.Item.RelativePath == path)
        {
            return node;
        }

        var relativeToRoot = path.RelativeTo(node.Item.RelativePath);

        foreach (var part in relativeToRoot.Parts)
        {
            var nextNode = node.Children.FirstOrDefault(child => child.Item.RelativePath.FileName == part);
            if (nextNode is null)
            {
                return Optional<ModContentNode>.None;
            }

            node = nextNode;
        }

        return node;
    }

    /// <summary>
    /// Attempts to find the node in the tree roots given it's <paramref name="path"/>.
    /// </summary>
    /// <param name="roots">Collection of the root nodes.</param>
    /// <param name="path">The <see cref="GamePath"/> of the node to find.</param>
    /// <returns>Optional containing the node if found.</returns>
    public static Optional<PreviewNode> GetTreeNode(this ReadOnlyObservableCollection<PreviewNode> roots, GamePath path)
    {
        var currentNode = roots.FirstOrDefault(node => node.Item.GamePath.GetRootComponent == path.GetRootComponent);
        if (currentNode is null)
        {
            return Optional<PreviewNode>.None;
        }

        if (path == currentNode.Item.GamePath)
        {
            return currentNode;
        }

        foreach (var part in path.Parts)
        {
            var nextNode = currentNode.Children.FirstOrDefault(node => node.Item.GamePath.FileName == part);
            if (nextNode is null)
            {
                return Optional<PreviewNode>.None;
            }

            currentNode = nextNode;
        }

        return currentNode;
    }

    /// <summary>
    /// Attempts to find the node in the tree roots given it's <paramref name="path"/>.
    /// </summary>
    /// <param name="roots">Collection of the root nodes.</param>
    /// <param name="path">The <see cref="GamePath"/> of the node to find.</param>
    /// <returns>Optional containing the node if found.</returns>
    public static Optional<SelectableNode> GetTreeNode(this ReadOnlyObservableCollection<SelectableNode> roots,
        GamePath path)
    {
        var currentNode = roots.FirstOrDefault(node => node.Item.GamePath.GetRootComponent == path.GetRootComponent);
        if (currentNode is null)
        {
            return Optional<SelectableNode>.None;
        }

        if (path == currentNode.Item.GamePath)
        {
            return currentNode;
        }

        foreach (var part in path.Parts)
        {
            var nextNode = currentNode.Children.FirstOrDefault(node => node.Item.GamePath.FileName == part);
            if (nextNode is null)
            {
                return Optional<SelectableNode>.None;
            }

            currentNode = nextNode;
        }

        return currentNode;
    }
}
