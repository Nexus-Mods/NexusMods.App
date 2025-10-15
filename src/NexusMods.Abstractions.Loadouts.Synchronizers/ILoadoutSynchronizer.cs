using DynamicData.Kernel;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Loadouts.Files.Diff;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.Sdk.Jobs;
using OneOf;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// A Loadout Synchronizer is responsible for synchronizing loadouts between to and from the game folder.
/// </summary>
public interface ILoadoutSynchronizer
{
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

    Dictionary<GamePath, FileConflictGroup> GetFileConflicts(Loadout.ReadOnly loadout, bool removeDuplicates = true);
    Dictionary<LoadoutItemGroup.ReadOnly, LoadoutFile.ReadOnly[]> GetFileConflictsByParentGroup(Loadout.ReadOnly loadout, bool removeDuplicates = true);

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

    public bool ShouldSynchronize(Loadout.ReadOnly loadout, IEnumerable<PathPartPair> previousState, IEnumerable<PathPartPair> lastScannedState);
    
    /// <summary>
    /// Computes the difference between a loadout and a disk state, assuming the loadout to be the newer state.
    /// </summary>x
    /// <param name="loadout">Newer state, e.g. unapplied loadout</param>
    /// <param name="previousState">The old state, e.g. last applied DiskState</param>
    /// <param name="lastScannedState">The last scanned state, e.g. the last time the game folder was scanned</param>
    /// <returns>A tree of all the files with associated <see cref="FileChangeType"/></returns>
    FileDiffTree LoadoutToDiskDiff(Loadout.ReadOnly loadout, List<PathPartPair> previousState, List<PathPartPair> lastScannedState);

    Task<GameInstallMetadata.ReadOnly> ReindexState(GameInstallation installation);
    ValueTask BuildProcessRun(Loadout.ReadOnly loadout, GameInstallMetadata.ReadOnly state, CancellationToken cancellationToken);

    Task ResetToOriginalGameState(GameInstallation installation, LocatorId[] locatorIds);

    /// <summary>
    /// Returns true if the path should be ignored by the synchronizer when backing up or restoring files. This does not mean
    /// that files on the given path will not be managed or moddable, just that the default files on that path are not backed
    /// up. This method is ignored when the global configuration is set to always back up all game files.
    /// </summary>
    bool IsIgnoredBackupPath(GamePath path);
}

public record struct FileConflictGroup(GamePath Path, FileConflictItem[] Items);
public record struct FileConflictItem(bool IsEnabled, OneOf<LoadoutFile.ReadOnly, DeletedFile.ReadOnly> File);
