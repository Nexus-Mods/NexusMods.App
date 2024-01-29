using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Abstractions.DiskState;

/// <summary>
/// A registry for managing disk states created by ingesting/applying loadouts
/// </summary>
public interface IDiskStateRegistry
{
    /// <summary>
    /// Saves a disk state to the data store
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <param name="diskState"></param>
    /// <returns></returns>
    void SaveState(LoadoutId loadoutId, DiskStateTree diskState);

    /// <summary>
    /// Gets the disk state associated with a specific version of a loadout (if any)
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <returns></returns>
    DiskStateTree? GetState(LoadoutId loadoutId);
}
