using System.Diagnostics;
using DynamicData.Kernel;
using NexusMods.Abstractions.GameLocators;
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
        if (GameInstallMetadata.LastSyncedLoadout.TryGetValue(metadata, out var lastApplied))
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

        var ret = DiskStateEntry.FindByGame(asOfDb, metadata.Id);
        return ret;
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
            return new(new EntityIds { Data = new byte[sizeof(uint)] }, metadata.Db);
        }

        return metadata.DiskStateAsOf(metadata.LastSyncedLoadoutTransaction);
    }
}
