using System.Collections.ObjectModel;
using DynamicData.Binding;
using DynamicData.Kernel;
using NexusMods.Games.AdvancedInstaller.UI.ModContent;
using NexusMods.Games.AdvancedInstaller.UI.Preview;
using NexusMods.Games.AdvancedInstaller.UI.SelectLocation;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI;

using ModContentNode = TreeNodeVM<IModContentTreeEntryViewModel, RelativePath>;
using PreviewNode = TreeNodeVM<IPreviewTreeEntryViewModel, GamePath>;
using SelectableNode = TreeNodeVM<ISelectableTreeEntryViewModel, GamePath>;

public static class ObservableTreesExtensions
{
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

    public static Optional<SelectableNode> GetTreeNode(this ReadOnlyObservableCollection<SelectableNode> roots, GamePath path)
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
