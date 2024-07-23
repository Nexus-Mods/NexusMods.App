using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Trees;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

public class SyncTree : AGamePathNodeTree<SyncTreeNode>
{
    public SyncTree(IEnumerable<KeyValuePair<GamePath, SyncTreeNode>> items) : base(items)
    {
    }
}
