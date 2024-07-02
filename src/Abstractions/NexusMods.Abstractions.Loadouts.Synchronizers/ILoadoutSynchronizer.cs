﻿using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Ids;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// A Loadout Synchronizer is responsible for synchronizing loadouts between to and from the game folder.
/// </summary>
public interface ILoadoutSynchronizer
{
    
    #region Synchronization Methods
    
    /// <summary>
    /// Creates a new sync tree from the current state of the game folder, the loadout and the previous state. This
    /// sync tree contains a matching of all the files in all 3 sources based on their path.
    /// </summary>
    SyncTree BuildSyncTree(DiskStateTree currentState, DiskStateTree previousTree, Loadout.ReadOnly loadoutTree);
    
    /// <summary>
    /// Builds a sync tree from a loadout and the current state of the game folder.
    /// </summary>
    /// <param name="loadoutTree"></param>
    /// <returns></returns>
    Task<SyncTree> BuildSyncTree(Loadout.ReadOnly loadoutTree);
    
    /// <summary>
    /// Processes the sync tree to create the signature and actions for each file, return a groupings object for the tree
    /// </summary>
    SyncActionGroupings ProcessSyncTree(SyncTree syncTree);
    
    /// <summary>
    /// Run the groupings on the game folder and return a new loadout with the changes applied.
    /// </summary>
    Task<Loadout.ReadOnly> RunGroupings(SyncTree tree, SyncActionGroupings groupings, Loadout.ReadOnly install);
    
    /// <summary>
    /// Synchronizes the loadout with the game folder, any changes in the game folder will be added to the loadout, and any
    /// new changes in the loadout will be applied to the game folder.
    /// </summary>
    Task<Loadout.ReadOnly> Synchronize(Loadout.ReadOnly loadout);
    
    /// <summary>
    /// Gets the current disk state of the game folders for the given game installation.
    /// </summary>
    Task<DiskStateTree> GetDiskState(GameInstallation installationInstance);

    /// <summary>
    /// Gets the current disk state of the game folders for the given loadout.
    /// </summary>
    Task<DiskStateTree> GetDiskState(Loadout.ReadOnly loadout)
    {
        return GetDiskState(loadout.InstallationInstance);
    }
    
    #endregion
    
    
    #region High Level Methods
    
    /// <summary>
    /// Computes the difference between a loadout and a disk state, assuming the loadout to be the newer state.
    /// </summary>
    /// <param name="loadout">Newer state, e.g. unapplied loadout</param>
    /// <param name="diskState">The old state, e.g. last applied DiskState</param>
    /// <returns>A tree of all the files with associated <see cref="FileChangeType"/></returns>
    FileDiffTree LoadoutToDiskDiff(Loadout.ReadOnly loadout, DiskStateTree diskState);
    
    /// <summary>
    /// Creates a loadout for a game, managing the game if it has not previously
    /// been managed.
    /// </summary>
    /// <param name="installation">The installation which should be managed.</param>
    /// <param name="suggestedName">Suggested friendly name for the 'Game Files' mod.</param>
    /// <returns></returns>
    /// <remarks>
    ///     This was formerly called 'Manage'.
    /// </remarks>
    Task<Loadout.ReadOnly> CreateLoadout(GameInstallation installation, string? suggestedName=null);

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
    /// Removes all the loadouts for a game, and resets the game folder to its
    /// initial state.
    /// </summary>
    /// <param name="installation">Game installation which should be unmanaged.</param>
    Task UnManage(GameInstallation installation);

    /// <summary>
    /// Returns true if the path should be ignored by the synchronizer when backing up or restoring files. This does not mean
    /// that files on the given path will not be managed or moddable, just that the default files on that path are not backed
    /// up. This method is ignored when the global configuration is set to always back up all game files.
    /// </summary>
    bool IsIgnoredBackupPath(GamePath path);

#endregion
}
