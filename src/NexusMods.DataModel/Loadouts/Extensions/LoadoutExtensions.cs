using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;

namespace NexusMods.DataModel.Loadouts.Extensions;

public static class LoadoutExtensions
{
    /// <summary>
    /// Helper method to apply a loadout to the game folder, calls the <see cref="ILoadoutSynchronizer.Apply"/> method,
    /// on the loadout's game's synchronizer.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    public static Task<DiskStateTree> Apply(this Loadout.Model loadout)
    {
        return loadout.Installation.GetGame().Synchronizer.Apply(loadout);
    }

    /// <summary>
    /// Helper method to ingest changes from the game folder into the loadout, calls the <see cref="ILoadoutSynchronizer.Ingest"/> method,
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    public static Task<Loadout.Model> Ingest(this Loadout.Model loadout)
    {
        return loadout.Installation.GetGame().Synchronizer.Ingest(loadout);
    }

}
