using System.Collections.Frozen;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.DataModel.Undo;

/// <summary>
/// Service for undoing loadout changes, getting restore points, etc. 
/// </summary>
public class UndoService
{
    private readonly IConnection _conn;

    /// <summary>
    /// DI constructor 
    /// </summary>
    /// <param name="connection"></param>
    public UndoService(IConnection connection)
    {
        _conn = connection;
    }

    /// <summary>
    /// Get a query of all the valid restore points (revisions) for the given loadout.
    /// </summary>
    public List<LoadoutRevisionWithStats> RevisionsFor(EntityId loadout)
    {
        var revisions = _conn.Query<(EntityId TxId, DateTimeOffset Timestamp)>($"SELECT Id, Timestamp FROM undo.LoadoutRevisionsWithMetadata({_conn}, {loadout})")
            .Select(row => LoadoutStats(_conn, new LoadoutRevision(loadout, row.TxId, row.Timestamp)))
            .ToList();
        return revisions;
    }
    
    /// <summary>
    /// This will be expanded in the future to diff loadout states, but for now just grab the mod count
    /// </summary>
    private static LoadoutRevisionWithStats LoadoutStats(IConnection conn, LoadoutRevision revision)
    {
        var newDb = conn.AsOf(TxId.From(revision.TxEntity.Value));
        var result = newDb.Connection.Query<long>($"SELECT COUNT(*) FROM mdb_LibraryLinkedLoadoutItem(Db=>{newDb}) WHERE Loadout = {revision.EntityId}").First();
        return new LoadoutRevisionWithStats(revision, (int)result); 
    }
    
    /// <summary>
    /// Attributes to ignore when undoing loadouts. Any of these attributes will not be reverted, and the entities pointed to
    /// in the E or V parts of the datom will not be traversed during the revert process.
    /// </summary>
    private static readonly IAttribute[] IgnoreAttributes =
    [
        GameInstallMetadata.LastSyncedLoadoutId,
        GameInstallMetadata.LastSyncedLoadoutTransactionId,
        DiskStateEntry.GameId,
        Loadout.LastAppliedDateTime,
        ManagedCollectionLoadoutGroup.Collection,
    ];

    /// <summary>
    /// Reverts the given loadout to the given revision.
    /// </summary>
    public async Task RevertTo(LoadoutRevision revisionRevision)
    {
        var currentDb = _conn.Db;
        var revertDb = _conn.AsOf(TxId.From(revisionRevision.TxEntity.Value));
        var ignoreAttrs = IgnoreAttributes.Select(a => currentDb.AttributeResolver.AttributeCache[a]).ToFrozenSet();
        var toProcess = new HashSet<EntityId>();
        toProcess.Add(revisionRevision.TxEntity);

        var tx = _conn.BeginTransaction();
        var processed = new HashSet<EntityId>();
        
        while (toProcess.Count > 0)
        {
            var current = toProcess.First();
            toProcess.Remove(current);

            // Skip if the entity has already been processed
            if (!processed.Add(current))
                continue;
            
            // Skip if the entity is a transaction entity
            if (current.Partition == PartitionId.Transactions)
                continue;


            var desiredState = revertDb[current];
            var currentState = currentDb[current];


            CompareEntities(current, currentState, processed, toProcess, desiredState, tx, ignoreAttrs);
            ExtractBackReferences(currentDb, current, ignoreAttrs, processed, toProcess, revertDb);
        }

        // Mark the transaction as a snapshot
        tx.Add(EntityId.From(TxId.Tmp.Value), LoadoutSnapshot.Snapshot, revisionRevision.EntityId);

        await tx.Commit();
    }

    private static void ExtractBackReferences(IDb currentDb, EntityId current, FrozenSet<AttributeId> ignoreAttrs, HashSet<EntityId> processed, HashSet<EntityId> toProcess, IDb revertDb)
    {
        var backReferenceDatoms = currentDb.Datoms(SliceDescriptor.CreateReferenceTo(current));
        foreach (var datom in backReferenceDatoms)
        {
            if (ignoreAttrs.Contains(datom.A) || processed.Contains(datom.E) || datom.E.Partition == PartitionId.Transactions)
                continue;
            
            toProcess.Add(datom.E);
        }
                
        var backReferenceDesiredState = revertDb.Datoms(SliceDescriptor.CreateReferenceTo(current));
        foreach (var datom in backReferenceDesiredState)
        {
            if (ignoreAttrs.Contains(datom.A) || processed.Contains(datom.E)  || datom.E.Partition == PartitionId.Transactions)
                continue;
            toProcess.Add(datom.E);
        }
    }

    private static void CompareEntities(EntityId current, Datoms currentState, HashSet<EntityId> processed, HashSet<EntityId> toProcess, Datoms desiredState, Transaction tx, FrozenSet<AttributeId> ignoreAttrs)
    {
        
        foreach (var datom in currentState)
        {
            if (ignoreAttrs.Contains(datom.A))
                continue;
            
            if (datom is { Tag: ValueTag.Reference, V: EntityId value })
            {
                if (!processed.Contains(value) && value.Partition != PartitionId.Transactions)
                    toProcess.Add(value);
            }
            
            if (!desiredState.Contains(datom)) 
                tx.Add(new Datom(datom.Prefix with { E = current, IsRetract = true}, datom.V));
        }

        foreach (var datom in desiredState)
        {
            if (ignoreAttrs.Contains(datom.A))
                continue;
            
            if (datom.Tag == ValueTag.Reference && datom.V is EntityId value)
            {
                if (!processed.Contains(value) && value.Partition != PartitionId.Transactions) 
                    toProcess.Add(value);
            }

            if (!currentState.Contains(datom))
                tx.Add(new Datom(datom.Prefix with { E = current }, datom.V));
        }
    }
}
