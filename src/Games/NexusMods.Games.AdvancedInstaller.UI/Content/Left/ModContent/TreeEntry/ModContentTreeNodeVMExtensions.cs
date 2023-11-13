using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.ModContent;

using ModContentNode = TreeNodeVM<IModContentTreeEntryViewModel, RelativePath>;

public static class ModContentTreeNodeVMExtensions
{
    public static ModContentNode? FindNode(this ModContentNode node, RelativePath id)
    {
        if (node.Id == id)
        {
            return node;
        }

        if (!id.InFolder(node.Id))
        {
            return null;
        }

        foreach (var child in node.Children)
        {
            var result = FindNode(child, id);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
