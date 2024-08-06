using DynamicData.Kernel;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Extensions.Hashing;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Extensions to the GameMetadata class to handle disk state
/// </summary>
public static class DiskStateExtensions
{

    /// <summary>
    /// Returns the last synchronized loadout for the game, if any
    /// </summary>
    public static Optional<LoadoutId> LastSynchronizedLoadout(this GameInstallMetadata.ReadOnly metadata)
    {
        if (GameInstallMetadata.LastSyncedLoadout.TryGet(metadata, out var lastApplied))
            return LoadoutId.From(lastApplied);
        return Optional<LoadoutId>.None;
    }

    
    /// <summary>
    /// Gets the latest game metadata for the installation
    /// </summary>
    public static GameInstallMetadata.ReadOnly GetMetadata(this GameInstallation installation, IConnection connection)
    {
        return GameInstallMetadata.Load(connection.Db, installation.GameMetadataId);
    }

    /// <summary>
    /// Gets the disk state of the game as of a specific transaction
    /// </summary>
    /// <param name="metadata"></param>
    /// <param name="txId"></param>
    /// <returns></returns>
    public static Entities<DiskStateEntry.ReadOnly> DiskStateAsOf(this GameInstallMetadata.ReadOnly metadata, TxId txId)
    {
        // Get an as-of db for the last applied loadout
        var asOfDb = metadata.Db.Connection.AsOf(txId);
        // Get the attributes for the entries in the disk state
        var segment = asOfDb.Datoms(DiskStateEntry.Game, metadata.Id);
        return new Entities<DiskStateEntry.ReadOnly>(new EntityIds(segment, 0, segment.Count), asOfDb);
    }
    
    /// <summary>
    /// Gets the disk state of the game as of a specific transaction
    /// </summary>
    public static Entities<DiskStateEntry.ReadOnly> DiskStateAsOf(this GameInstallMetadata.ReadOnly metadata, Transaction.ReadOnly tx)
    {
        return DiskStateAsOf(metadata, TxId.From(tx.Id.Value));
    }
    
    /// <summary>
    /// Load the disk state of the game as of the last applied loadout
    /// </summary>
    public static Entities<DiskStateEntry.ReadOnly> GetLastAppliedDiskState(this GameInstallMetadata.ReadOnly metadata)
    {
        // No previously applied loadout, return an empty state
        if (!metadata.Contains(GameInstallMetadata.LastSyncedLoadout))
        {
            return EmptyState;
        }
        return metadata.DiskStateAsOf(metadata.LastSyncedLoadoutTransaction);
    }
    
    private static readonly Entities<DiskStateEntry.ReadOnly> EmptyState = new();
    
    /// <summary>
    /// Reindex the state of the game, running a transaction if changes are found
    /// </summary>
    public static async Task<GameInstallMetadata.ReadOnly> ReindexState(this GameInstallation installation, IConnection connection)
    {
        var originalMetadata = GetMetadata(installation, connection);
        using var tx = connection.BeginTransaction();

        // Index the state
        var changed = await installation.ReindexState(connection, tx);
        
        if (!originalMetadata.Contains(GameInstallMetadata.InitialStateTransaction))
        {
            // No initial state, so set this transaction as the initial state
            changed = true;
            tx.Add(originalMetadata.Id, GameInstallMetadata.InitialStateTransaction, EntityId.From(TxId.Tmp.Value));
        }
        
        if (changed)
        {
            await tx.Commit();
        }
        
        return GameInstallMetadata.Load(connection.Db, installation.GameMetadataId);
    }
    
    /// <summary>
    /// Reindex the state of the game
    /// </summary>
    public static async Task<bool> ReindexState(this GameInstallation installation, IConnection connection, ITransaction tx)
    {
        var seen = new HashSet<GamePath>();
        var metadata = GameInstallMetadata.Load(connection.Db, installation.GameMetadataId);
        var inState = metadata.DiskStateEntries.ToDictionary(e => (GamePath)e.Path);
        var changes = false;
        
        foreach (var location in installation.LocationsRegister.GetTopLevelLocations())
        {
            if (!location.Value.DirectoryExists())
                continue;

            await Parallel.ForEachAsync(location.Value.EnumerateFiles(), async (file, token) =>
                {
                    {
                        var gamePath = installation.LocationsRegister.ToGamePath(file);
                        
                        lock (seen)
                        {
                            seen.Add(gamePath);
                        }

                        if (inState.TryGetValue(gamePath, out var entry))
                        {
                            var fileInfo = file.FileInfo;

                            // If the files don't match, update the entry
                            if (fileInfo.LastWriteTimeUtc > entry.LastModified || fileInfo.Size != entry.Size)
                            {
                                var newHash = await file.XxHash64Async();
                                tx.Add(entry.Id, DiskStateEntry.Size, fileInfo.Size);
                                tx.Add(entry.Id, DiskStateEntry.Hash, newHash);
                                tx.Add(entry.Id, DiskStateEntry.LastModified, fileInfo.LastWriteTimeUtc);
                                changes = true;
                            }
                        }
                        else
                        {
                            // No previous entry found, so create a new one
                            var newHash = await file.XxHash64Async(token: token);
                            _ = new DiskStateEntry.New(tx, tx.TempId(DiskStateEntry.EntryPartition))
                            {
                                Path = gamePath.ToGamePathParentTuple(metadata.Id),
                                Hash = newHash,
                                Size = file.FileInfo.Size,
                                LastModified = file.FileInfo.LastWriteTimeUtc,
                                GameId = metadata.Id,
                            };
                            changes = true;
                        }
                    }
                }
            );
        }
        
        foreach (var entry in inState.Values)
        {
            if (seen.Contains(entry.Path))
                continue;
            tx.Retract(entry.Id, DiskStateEntry.Path, entry.Path);
            tx.Retract(entry.Id, DiskStateEntry.Hash, entry.Hash);
            tx.Retract(entry.Id, DiskStateEntry.Size, entry.Size);
            tx.Retract(entry.Id, DiskStateEntry.LastModified, entry.LastModified);
            tx.Retract(entry.Id, DiskStateEntry.Game, metadata.Id);
            changes = true;
        }
        
        
        if (changes) 
            tx.Add(metadata.Id, GameInstallMetadata.LastScannedDiskStateTransaction, EntityId.From(TxId.Tmp.Value));
        
        return changes;
    }
        
    /// <summary>
    /// Index the game state and create the initial disk state
    /// </summary>
    public static async Task IndexNewState(this GameInstallation installation, ITransaction tx)
    {
        var metaDataId = installation.GameMetadataId;
        
        foreach (var location in installation.LocationsRegister.GetTopLevelLocations())
        {
            if (!location.Value.DirectoryExists())
                continue;
            foreach (var file in location.Value.EnumerateFiles())
            {
                var gamePath = installation.LocationsRegister.ToGamePath(file);
                var newHash = await file.XxHash64Async();
                _ = new DiskStateEntry.New(tx, tx.TempId(DiskStateEntry.EntryPartition))
                {
                    Path = gamePath.ToGamePathParentTuple(metaDataId),
                    Hash = newHash,
                    Size = file.FileInfo.Size,
                    LastModified = file.FileInfo.LastWriteTimeUtc,
                    GameId = metaDataId
                };
            }
        }
    }

    
}
