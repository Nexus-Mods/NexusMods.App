using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.DataModel.LoadoutSynchronizer;

/// <summary>
/// A tree representing the current state of files on disk.
/// </summary>
public class DiskState : AGamePathTree<HashedEntry>
{
    private DiskState(IEnumerable<KeyValuePair<GamePath, HashedEntry>> tree) : base(tree) { }

    public static DiskState Create(IEnumerable<KeyValuePair<GamePath, HashedEntry>> tree)
    {
        return new DiskState(tree);
    }


}
