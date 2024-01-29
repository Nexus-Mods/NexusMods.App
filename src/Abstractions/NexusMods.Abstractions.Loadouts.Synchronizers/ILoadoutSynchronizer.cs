namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// A Loadout Synchronizer is responsible for synchronizing loadouts between to and from the game folder.
/// </summary>
public interface ILoadoutSynchronizer
{
    #region Merge Methods

    /// <summary>
    /// Merges two loadouts together, creating a new loadout.
    /// </summary>
    /// <param name="loadoutA"></param>
    /// <param name="loadoutB"></param>
    /// <returns></returns>
    Loadout MergeLoadouts(Loadout loadoutA, Loadout loadoutB);

    #endregion

    #region High Level Methods
    /// <summary>
    /// Applies a loadout to the game folder.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns>The new DiskState after the files were applied</returns>
    Task<DiskState> Apply(Loadout loadout);

    /// <summary>
    /// Ingests changes from the game folder into the loadout.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    Task<Loadout> Ingest(Loadout loadout);

    /// <summary>
    /// Manage a game, creating the initial loadout
    /// </summary>
    /// <param name="installation"></param>
    /// <returns></returns>
    Task<Loadout> Manage(GameInstallation installation);
    #endregion
}
