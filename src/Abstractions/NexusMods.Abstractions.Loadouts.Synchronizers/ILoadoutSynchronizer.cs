﻿using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// A Loadout Synchronizer is responsible for synchronizing loadouts between to and from the game folder.
/// </summary>
public interface ILoadoutSynchronizer
{
    
    #region Diff Methods
    
    /// <summary>
    /// Computes the difference between a loadout and a disk state, assuming the loadout to be the newer state.
    /// </summary>
    /// <param name="loadout">Newer state, e.g. unapplied loadout</param>
    /// <param name="diskState">The old state, e.g. last applied DiskState</param>
    /// <returns>A tree of all the files with associated <see cref="FileChangeType"/></returns>
    ValueTask<FileDiffTree> LoadoutToDiskDiff(Loadout.Model loadout, DiskStateTree diskState);
    
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
    Task<DiskStateTree> Apply(Loadout.Model loadout, bool forceSkipIngest = false);

    /// <summary>
    /// Ingests changes from the game folder into the loadout.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    Task<Loadout.Model> Ingest(Loadout.Model loadout);

    /// <summary>
    /// Manage a game, creating the initial loadout
    /// </summary>
    /// <param name="installation"></param>
    /// <returns></returns>
    Task<Loadout.Model> Manage(GameInstallation installation, string? suggestedName=null);
    #endregion
}
