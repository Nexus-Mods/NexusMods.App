using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Abstractions.DiskState;

/// <summary>
/// A registry for managing disk states created by ingesting/applying loadouts
/// </summary>
public interface IDiskStateRegistry
{
    /// <summary>
    /// Saves a disk state to the data store for the given game installation
    /// </summary>
    /// <returns></returns>
    void SaveState(GameInstallation installation, DiskStateTree diskState);

    /// <summary>
    /// Gets the disk state associated with a specific game installation, returns false if no state is found
    /// </summary>
    /// <returns></returns>
    DiskStateTree? GetState(GameInstallation gameInstallation);
}
