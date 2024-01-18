using NexusMods.Abstractions.Games.DTO;

namespace NexusMods.Abstractions.Games.Loadouts;

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
    void SaveState(LoadoutId loadoutId, DiskState diskState);

    /// <summary>
    /// Gets the disk state associated with a specific version of a loadout (if any)
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <returns></returns>
    DiskState? GetState(LoadoutId loadoutId);
}
