using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;

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
    
    #region Diff Methods
    
    /// <summary>
    /// Computes the difference between a loadout and a disk state, assuming the loadout to be the newer state.
    /// </summary>
    /// <param name="loadout">Newer state, e.g. unapplied loadout</param>
    /// <param name="diskState">The old state, e.g. last applied DiskState</param>
    /// <returns>A tree of all the files with associated <see cref="FileChangeType"/></returns>
    ValueTask<FileDiffTree> LoadoutToDiskDiff(Loadout loadout, DiskStateTree diskState);
    
    #endregion

    #region High Level Methods
    /// <summary>
    /// Applies a loadout to the game folder.
    /// </summary>
    /// <param name="loadout"></param>
    /// <param name="forceSkipIngest">
    ///     Skips checking if an ingest is needed.
    ///     Force overrides current locations to intended tree
    /// </param>
    /// <returns>The new DiskState after the files were applied</returns>
    Task<DiskStateTree> Apply(Loadout loadout, bool forceSkipIngest = false);

    /// <summary>
    /// Ingests changes from the game folder into the loadout.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    Task<Loadout> Ingest(Loadout loadout);

    /// <summary>
    /// Creates a loadout for a game, managing the game if it has not previously
    /// been managed.
    /// </summary>
    /// <param name="installation"></param>
    /// <param name="suggestedName">Suggested friendly name for the 'Game Files' mod.</param>
    /// <returns></returns>
    /// <remarks>
    ///     This was formerly called 'Manage'.
    /// </remarks>
    Task<Loadout> CreateLoadout(GameInstallation installation, string? suggestedName=null);

    /// <summary>
    /// Deletes the loadout for the game. If the loadout is the currently active loadout,
    /// the game's folder will be reset to its initial state.
    /// </summary>
    /// <param name="installation">The installation for which the loadout should be deleted.</param>
    /// <param name="loadoutId">Unique identifier for the loadout.</param>
    /// <returns></returns>
    /// <remarks>
    ///     If there is only one loadout for this game, the initial game state is removed,
    ///     in other words, a full <see cref="UnManage"/> is performed.
    /// </remarks>
    Task DeleteLoadout(GameInstallation installation, LoadoutId loadoutId);

    /// <summary>
    /// Removes all of the loadouts for a game, an resets the game folder to its
    /// initial state.
    /// </summary>
    /// <param name="installation">Game installation which should be unmanaged.</param>
    Task UnManage(GameInstallation installation);
#endregion
}
