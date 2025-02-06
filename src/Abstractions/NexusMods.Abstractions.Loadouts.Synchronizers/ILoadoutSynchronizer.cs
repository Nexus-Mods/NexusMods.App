using DynamicData.Kernel;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts.Files.Diff;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

using DiskState = Entities<DiskStateEntry.ReadOnly>;

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
    void MergeStates(IEnumerable<PathPartPair> currentState, IEnumerable<PathPartPair> previousTree, Dictionary<GamePath, SyncNode> loadoutItems);
    
    /// <summary>
    /// Builds a sync tree from a loadout and the current state of the game folder.
    /// </summary>
    Task<Dictionary<GamePath, SyncNode>> BuildSyncTree(Loadout.ReadOnly loadoutTree);
    
    /// <summary>
    /// Processes the sync tree to create the signature and actions for each file, changes are made in-place on the tree.
    /// </summary>
    void ProcessSyncTree(Dictionary<GamePath, SyncNode> syncTree);
    
    /// <summary>
    /// Run the groupings on the game folder and return a new loadout with the changes applied.
    /// </summary>
    Task<Loadout.ReadOnly> RunActions(Dictionary<GamePath, SyncNode> syncTree, Loadout.ReadOnly loadout);
    
    /// <summary>
    /// Synchronizes the loadout with the game folder, any changes in the game folder will be added to the loadout, and any
    /// new changes in the loadout will be applied to the game folder.
    /// </summary>
    Task<Loadout.ReadOnly> Synchronize(Loadout.ReadOnly loadout);
    
    
    /// <summary>
    /// Rescan the files in the folders this game requires. This is used to bring the local cache up to date with the
    /// whatever is on disk.
    /// </summary>
    Task<GameInstallMetadata.ReadOnly> RescanFiles(GameInstallation gameInstallation);
    
    #endregion
    
    
    #region High Level Methods
    
    /// <summary>
    /// Computes the difference between a loadout and a disk state, assuming the loadout to be the newer state.
    /// </summary>x
    /// <param name="loadout">Newer state, e.g. unapplied loadout</param>
    /// <param name="diskState">The old state, e.g. last applied DiskState</param>
    /// <returns>A tree of all the files with associated <see cref="FileChangeType"/></returns>
    FileDiffTree LoadoutToDiskDiff(Loadout.ReadOnly loadout, DiskState diskState);
    
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
    IJobTask<CreateLoadoutJob, Loadout.ReadOnly> CreateLoadout(GameInstallation installation, string? suggestedName=null);

    /// <summary>
    /// Resets a game back to it's initial state, any applied loadouts will be unapplied.
    /// </summary>
    public Task DeactivateCurrentLoadout(GameInstallation installation);
    
    /// <summary>
    /// Gets the currently active loadout for the game, if any.
    /// </summary>
    /// <param name="installation"></param>
    /// <returns></returns>
    public Optional<LoadoutId> GetCurrentlyActiveLoadout(GameInstallation installation);
    
    /// <summary>
    /// Sets the loadout as the active loadout for the game, applying the changes to the game folder.
    /// </summary>
    public Task ActivateLoadout(LoadoutId loadout);

    /// <summary>
    /// Deletes the loadout for the game. If the loadout is the currently active loadout,
    /// the game's folder will be reset to its initial state.
    /// </summary>
    Task DeleteLoadout(LoadoutId loadout, GarbageCollectorRunMode gcRunMode = GarbageCollectorRunMode.DoNotRun);

    /// <summary>
    /// Removes all the loadouts for a game, and resets the game folder to its
    /// initial state.
    /// </summary>
    /// <param name="installation">Game installation which should be unmanaged.</param>
    /// <param name="runGc">If true, runs the garbage collector.</param>
    Task UnManage(GameInstallation installation, bool runGc = true);

    /// <summary>
    /// Returns true if the path should be ignored by the synchronizer when backing up or restoring files. This does not mean
    /// that files on the given path will not be managed or moddable, just that the default files on that path are not backed
    /// up. This method is ignored when the global configuration is set to always back up all game files.
    /// </summary>
    bool IsIgnoredBackupPath(GamePath path);
    
    /// <summary>
    /// Returns true if the path should be ignored by the synchronizer when scanning for files. This means this folder is completely ignored
    /// by the synchronizer and will not be managed or moddable.
    /// </summary>
    bool IsIgnoredPath(GamePath path);
    
    

#endregion

    /// <summary>
    /// Create a clone of the current loadout
    /// </summary>
    Task<Loadout.ReadOnly> CopyLoadout(LoadoutId loadout);
}
