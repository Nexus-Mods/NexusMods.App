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
    /// Gets the latest game metadata for the installation
    /// </summary>
    public static GameMetadata.ReadOnly GetMetadata(this GameInstallation installation, IConnection connection)
    {
        return GameMetadata.Load(connection.Db, installation.GameMetadataId);
    }

    /// <summary>
    /// Gets the disk state of the game as of a specific transaction
    /// </summary>
    /// <param name="metadata"></param>
    /// <param name="txId"></param>
    /// <returns></returns>
    public static Entities<DiskStateEntry.ReadOnly> DiskStateAsOf(this GameMetadata.ReadOnly metadata, TxId txId)
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
    public static Entities<DiskStateEntry.ReadOnly> DiskStateAsOf(this GameMetadata.ReadOnly metadata, Transaction.ReadOnly tx)
    {
        return DiskStateAsOf(metadata, TxId.From(tx.Id.Value));
    }
    
    /// <summary>
    /// Load the disk state of the game as of the last applied loadout
    /// </summary>
    public static Entities<DiskStateEntry.ReadOnly> GetLastAppliedDiskState(this GameMetadata.ReadOnly metadata)
    {
        // No previously applied loadout, return an empty state
        if (!metadata.Contains(GameMetadata.LastAppliedLoadout))
        {
            return EmptyState;
        }
        return metadata.DiskStateAsOf(metadata.LastAppliedLoadoutTransaction);
    }
    
    private static readonly Entities<DiskStateEntry.ReadOnly> EmptyState = new();
    
    /// <summary>
    /// Reindex the state of the game, running a transaction if changes are found
    /// </summary>
    public static async Task<GameMetadata.ReadOnly> ReindexState(this GameInstallation installation, IConnection connection)
    {
        using var tx = connection.BeginTransaction();
        var changed = await installation.ReindexState(connection, tx);
        if (changed)
        {
            await tx.Commit();
        }
        return GameMetadata.Load(connection.Db, installation.GameMetadataId);
    }
    
    /// <summary>
    /// Reindex the state of the game
    /// </summary>
    public static async Task<bool> ReindexState(this GameInstallation installation, IConnection connection, ITransaction tx)
    {
        var seen = new HashSet<GamePath>();
        var metadata = GameMetadata.Load(connection.Db, installation.GameMetadataId);
        var inState = metadata.DiskStateEntries.ToDictionary(e => e.Path);
        bool changes = false;
        
        foreach (var location in installation.LocationsRegister.GetTopLevelLocations())
        {
            foreach (var file in location.Value.EnumerateFiles())
            {
                var gamePath = installation.LocationsRegister.ToGamePath(file);
                seen.Add(gamePath);
                
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
                    var newHash = await file.XxHash64Async();
                    _ = new DiskStateEntry.New(tx, tx.TempId(DiskStateEntry.EntryPartition))
                    {
                        Path = gamePath,
                        Hash = newHash,
                        Size = file.FileInfo.Size,
                        LastModified = file.FileInfo.LastWriteTimeUtc,
                        GameId = metadata.Id,
                    };
                    changes = true;
                }
            }
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
            tx.Add(metadata.Id, GameMetadata.LastScannedTransaction, EntityId.From(TxId.Tmp.Value));
        
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
            foreach (var file in location.Value.EnumerateFiles())
            {
                var gamePath = installation.LocationsRegister.ToGamePath(file);
                var newHash = await file.XxHash64Async();
                _ = new DiskStateEntry.New(tx, tx.TempId(DiskStateEntry.EntryPartition))
                {
                    Path = gamePath,
                    Hash = newHash,
                    Size = file.FileInfo.Size,
                    LastModified = file.FileInfo.LastWriteTimeUtc,
                    GameId = metaDataId
                };
            }
        }
    }

    
}
