using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.LoadoutSynchronizer.Extensions;

public static class LoadoutExtensions
{

    /// <summary>
    /// Assuming the game has a IStandardizedLoadoutSynchronizer, this will flatten the loadout into a FlattenedLoadout.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    public static ValueTask<FlattenedLoadout> ToFlattenedLoadout(this Loadout loadout)
    {
        return ((IStandardizedLoadoutSynchronizer)loadout.Installation.Game.Synchronizer).LoadoutToFlattenedLoadout(loadout);
    }

    /// <summary>
    /// Assuming the game has a IStandardizedLoadoutSynchronizer, this will flatten the loadout into a FileTree.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    public static async ValueTask<FileTree> ToFileTree(this Loadout loadout)
    {
        var fileTree = await loadout.ToFlattenedLoadout();
        return await ((IStandardizedLoadoutSynchronizer)loadout.Installation.Game.Synchronizer)
            .FlattenedLoadoutToFileTree(fileTree, loadout);
    }

}
