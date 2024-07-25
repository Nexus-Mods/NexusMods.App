using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Trees;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

[Obsolete($"To be replaced by {nameof(SyncTree)}")]
public class SyncTreeOld : AGamePathNodeTree<SyncTreeNodeOld>
{
    public SyncTreeOld(IEnumerable<KeyValuePair<GamePath, SyncTreeNodeOld>> items) : base(items)
    {
    }
}
