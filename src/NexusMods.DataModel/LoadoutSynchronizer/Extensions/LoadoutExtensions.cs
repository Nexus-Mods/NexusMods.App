using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;

namespace NexusMods.DataModel.LoadoutSynchronizer.Extensions;

/// <summary>
/// Various extension methods for Loadouts and the loadout registry
/// </summary>
public static class LoadoutExtensions
{

    /// <summary>
    /// Assuming the game has a IStandardizedLoadoutSynchronizer, this will flatten the loadout into a FlattenedLoadout.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    public static ValueTask<FlattenedLoadout> ToFlattenedLoadout(this Loadout.Model loadout)
    {
        return ((IStandardizedLoadoutSynchronizer)loadout.Installation.GetGame().Synchronizer).LoadoutToFlattenedLoadout(loadout);
    }

    /// <summary>
    /// Assuming the game has a IStandardizedLoadoutSynchronizer, this will flatten the loadout into a FileTree.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    public static async ValueTask<FileTree> ToFileTree(this Loadout.Model loadout)
    {
        var fileTree = await loadout.ToFlattenedLoadout();
        return await ((IStandardizedLoadoutSynchronizer)loadout.Installation.GetGame().Synchronizer)
            .FlattenedLoadoutToFileTree(fileTree, loadout);
    }
}
