using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Serialization.DataModel.Ids;

namespace NexusMods.Abstractions.DiskState;

/// <summary>
/// A registry for managing disk states created by ingesting/applying loadouts
/// </summary>
public interface IDiskStateRegistry
{
    /// <summary>
    /// Saves a disk state to the data store for the given game installation
    /// </summary>
    Task SaveState(GameInstallation installation, DiskStateTree diskState);

    /// <summary>
    /// Gets the disk state associated with a specific game installation, returns false if no state is found
    /// </summary>
    /// <returns></returns>
    DiskStateTree? GetState(GameInstallation gameInstallation);
    
    /// <summary>
    /// Gets the Loadout Revision Id of the last applied state for a given game installation
    /// </summary>
    /// <param name="gameInstallation"></param>
    /// <returns></returns>
    IId? GetLastAppliedLoadout(GameInstallation gameInstallation);
    
    /// <summary>
    /// Observable of all the last applied revisions for all game installations
    /// </summary>
    IObservable<(GameInstallation gameInstallation, IId loadoutRevision)> LastAppliedRevisionObservable { get; }
}
