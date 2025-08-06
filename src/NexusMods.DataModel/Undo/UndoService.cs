using System.Collections.Frozen;
using System.Diagnostics;
using FomodInstaller.Interface;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Loadouts;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
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
        var sw = Stopwatch.StartNew();
        var revisions = _conn.Query<(EntityId TxId, DateTimeOffset Timestamp)>(Queries.LoadoutRevisionsWithMetadata, _conn, loadout)
            .Select(row => LoadoutStats(_conn, new LoadoutRevision(loadout, row.TxId, row.Timestamp)))
            .ToList();
        var elapsed = sw.ElapsedMilliseconds;
        return revisions;
    }
    
    /// <summary>
    /// This will be expanded in the future to diff loadout states, but for now just grab the mod count
    /// </summary>
    private static LoadoutRevisionWithStats LoadoutStats(IConnection conn, LoadoutRevision revision)
    {
        const string modCount = "SELECT COUNT(*) FROM mdb_LibraryLinkedLoadoutItem(Db=>$1) WHERE Loadout = $2";
        var newDb = conn.AsOf(TxId.From(revision.TxEntity.Value));
        var result = newDb.Connection.Query<long>(modCount, newDb, revision.EntityId).First();
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
        var ignoreAttrs = IgnoreAttributes.Select(a => currentDb.AttributeCache.GetAttributeId(a.Id)).ToFrozenSet();
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


            var desiredState = revertDb.Datoms(current);
            var currentState = currentDb.Datoms(current);


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
        var resolver = currentDb.AttributeCache;
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

    private static void CompareEntities(EntityId current, EntitySegment currentState, HashSet<EntityId> processed, HashSet<EntityId> toProcess, EntitySegment desiredState, ITransaction tx, FrozenSet<AttributeId> ignoreAttrs)
    {
        
        foreach (var avData in currentState.GetAVEnumerable())
        {
            if (ignoreAttrs.Contains(avData.A))
                continue;
            
            if (avData.ValueType == ValueTag.Reference)
            {
                var value = EntityIdSerializer.Read(avData.Value.Span);
                if (!processed.Contains(value) && value.Partition != PartitionId.Transactions)
                    toProcess.Add(value);
            }
                
            if (!desiredState.Contains(avData)) 
                tx.Add(current, avData.A, avData.ValueType, avData.Value.Span, true);
        }

        foreach (var datom in desiredState.GetAVEnumerable())
        {
            if (ignoreAttrs.Contains(datom.A))
                continue;
            
            if (datom.ValueType == ValueTag.Reference)
            {
                var value = EntityIdSerializer.Read(datom.Value.Span);
                if (!processed.Contains(value) && value.Partition != PartitionId.Transactions) 
                    toProcess.Add(value);
            }
                
            if (!currentState.Contains(datom)) 
                tx.Add(current, datom.A, datom.ValueType, datom.Value.Span);
        }
    }
}
