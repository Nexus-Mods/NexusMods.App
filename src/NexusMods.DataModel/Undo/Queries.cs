using NexusMods.Abstractions.Loadouts;
using NexusMods.Cascade;
using NexusMods.Cascade.Flows;
using NexusMods.Cascade.Patterns;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.Cascade;


namespace NexusMods.DataModel.Undo;

public static class Queries
{

    
    /// <summary>
    /// Implied loadout revisions, that we turn into snapshots to allow the user to undo to a previous working state
    /// </summary>
    public static readonly Flow<(EntityId Loadout, EntityId TxEntityId)> LoadoutAppliedRevsions =
        Pattern.Create()
            .DbHistory(out var loadoutId, Loadout.LastAppliedDateTime, out _, out var txEntity)
            .Return(loadoutId, txEntity);
    
    /// <summary>
    /// Reverted loadout operations, each time we revert a loadout we add a datom to the transaction to checkpoint the point where
    /// the loadout was reverted.
    /// </summary>
    private static readonly Flow<(EntityId Loadout, EntityId TxEntityId)> LoadoutSnapshots =
        Pattern.Create()
            .Db(out var txEntity, LoadoutSnapshot.Snapshot, out var loadoutId)
            .Return(loadoutId, txEntity)
            .Union()
            .With(LoadoutAppliedRevsions);

    /// <summary>
    /// Now join each snapshot to other metadata, like the transaction timestamp
    /// </summary>
    public static readonly Flow<LoadoutRevision> LoadoutRevisionsWithMetadata =
        Pattern.Create()
            .Match(LoadoutSnapshots, out var loadoutId, out var txEntity)
            .Db(txEntity, Transaction.Timestamp, out var timestamp)
            .ReturnLoadoutRevision(loadoutId, txEntity, timestamp);
    
}
