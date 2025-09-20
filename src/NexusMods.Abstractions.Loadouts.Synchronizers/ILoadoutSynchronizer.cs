using DynamicData.Kernel;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts.Files.Diff;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using OneOf;

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
    /// Builds a sync tree from the latest stored disk state and the previous disk state.
    /// </summary>
    Dictionary<GamePath, SyncNode> BuildSyncTree<T>(T latestDiskState, T previousDiskState, Loadout.ReadOnly loadout) where T : IEnumerable<PathPartPair>;
    
    /// <summary>
    /// Processes the sync tree to create the signature and actions for each file, changes are made in-place on the tree.
    /// </summary>
    void ProcessSyncTree(Dictionary<GamePath, SyncNode> syncTree);
    
    /// <summary>
    /// Run the groupings on the game folder and return a new loadout with the changes applied.
    /// </summary>
    Task<Loadout.ReadOnly> RunActions(Dictionary<GamePath, SyncNode> syncTree, Loadout.ReadOnly loadout, SynchronizeLoadoutJob? job = null);
    
    /// <summary>
    /// Synchronizes the loadout with the game folder, any changes in the game folder will be added to the loadout, and any
    /// new changes in the loadout will be applied to the game folder.
    /// </summary>
    Task<Loadout.ReadOnly> Synchronize(Loadout.ReadOnly loadout, SynchronizeLoadoutJob? job = null);

    Dictionary<GamePath, OneOf<LoadoutFile.ReadOnly, DeletedFile.ReadOnly>[]> GetFileConflicts(Loadout.ReadOnly loadout, bool removeDuplicates = true);

    /// <summary>
    /// Rescan the files in the folders this game requires. This is used to bring the local cache up to date with the
    /// whatever is on disk.
    /// </summary>
    /// <param name="gameInstallation">The game installation to rescan.</param>
    /// <param name="ignoreModifiedDate">
    /// If false, files that have unchanged modified date since the last scan will be skipped.
    /// If true, all files will be rehashed.
    /// </param>
    Task<GameInstallMetadata.ReadOnly> RescanFiles(GameInstallation gameInstallation, bool ignoreModifiedDate = false);

    /// <summary>
    /// Get the disk state for a game as of a specific transaction.
    /// </summary>
    /// <param name="metadata"></param>
    /// <param name="asOfTxId"></param>
    /// <returns></returns>
    public List<PathPartPair> GetDiskStateForGameAsOf(GameInstallMetadata.ReadOnly metadata, TxId asOfTxId)
    {
        var db = metadata.Db.Connection.AsOf(asOfTxId);
        var oldMetadata = GameInstallMetadata.Load(db, metadata.Id);
        return GetDiskStateForGame(oldMetadata);
    }
    
    

    /// <summary>
    /// Gets the previously applied disk state for a game.
    /// </summary>
    public List<PathPartPair> GetPreviouslyAppliedDiskState(GameInstallMetadata.ReadOnly metadata)
    {
        List<PathPartPair> prevItems;
        if (!metadata.Contains(GameInstallMetadata.LastSyncedLoadout))
        {
            prevItems = [];
        }
        else
        {
            var txId = GameInstallMetadata.LastSyncedLoadoutTransactionId.Get(metadata);
            var asOfDb = metadata.Db.Connection.AsOf(TxId.From(txId.Value));
            var oldMetadata = GameInstallMetadata.Load(asOfDb, metadata.Id);
            prevItems = GetDiskStateForGame(oldMetadata);
        }
        return prevItems;
    }
    
    /// <summary>
    /// Gets the previously applied disk state for a game.
    /// </summary>
    public List<PathPartPair> GetLastScannedDiskState(GameInstallMetadata.ReadOnly metadata)
    {
        List<PathPartPair> prevItems;
        if (!GameInstallMetadata.LastScannedDiskStateTransaction.TryGetValue(metadata, out var txId))
        {
            prevItems = [];
        }
        else
        {
            var asOfDb = metadata.Db.Connection.AsOf(TxId.From(txId.Value));
            var oldMetadata = GameInstallMetadata.Load(asOfDb, metadata.Id);
            prevItems = GetDiskStateForGame(oldMetadata);
        }
        return prevItems;
    }
    
    /// <summary>
    /// Get the disk state for a game from the given database.
    /// </summary>
    public List<PathPartPair> GetDiskStateForGame(GameInstallMetadata.ReadOnly metadata);
    
    #endregion
    
    
    #region High Level Methods
    
    public bool ShouldSynchronize(Loadout.ReadOnly loadout, IEnumerable<PathPartPair> previousState, IEnumerable<PathPartPair> lastScannedState);
    
    /// <summary>
    /// Computes the difference between a loadout and a disk state, assuming the loadout to be the newer state.
    /// </summary>x
    /// <param name="loadout">Newer state, e.g. unapplied loadout</param>
    /// <param name="previousState">The old state, e.g. last applied DiskState</param>
    /// <param name="lastScannedState">The last scanned state, e.g. the last time the game folder was scanned</param>
    /// <returns>A tree of all the files with associated <see cref="FileChangeType"/></returns>
    FileDiffTree LoadoutToDiskDiff(Loadout.ReadOnly loadout, List<PathPartPair> previousState, List<PathPartPair> lastScannedState);
    
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
    /// Last synced loadout should be cleared if the game is being reset
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
    Task DeleteLoadout(LoadoutId loadout, GarbageCollectorRunMode gcRunMode = GarbageCollectorRunMode.DoNotRun, bool deactivateIfActive = true);

    /// <summary>
    /// Removes all the loadouts for a game, and resets the game folder to its
    /// initial state.
    /// </summary>
    /// <param name="installation">Game installation which should be unmanaged.</param>
    /// <param name="runGc">If true, runs the garbage collector.</param>
    Task UnManage(GameInstallation installation, bool runGc = true, bool cleanGameFolder = true);

    /// <summary>
    /// Returns true if the path should be ignored by the synchronizer when backing up or restoring files. This does not mean
    /// that files on the given path will not be managed or moddable, just that the default files on that path are not backed
    /// up. This method is ignored when the global configuration is set to always back up all game files.
    /// </summary>
    bool IsIgnoredBackupPath(GamePath path);

#endregion

    /// <summary>
    /// Create a clone of the current loadout
    /// </summary>
    Task<Loadout.ReadOnly> CopyLoadout(LoadoutId loadout);
}
