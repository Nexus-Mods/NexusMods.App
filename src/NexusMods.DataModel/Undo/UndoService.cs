using System.Collections.Frozen;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Cascade;
using NexusMods.Cascade.Patterns;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Cascade;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.DataModel.Undo;

public class UndoService
{
    private readonly IConnection _conn;

    public UndoService(IConnection connection)
    {
        _conn = connection;
    }

    public async Task<OutletNode<LoadoutRevisionWithStats>> RevisionsFor(EntityId loadout)
    {
        return await _conn.Topology.OutletAsync(Queries.LoadoutRevisionsWithMetadata
            .Where(row => row.RowId == loadout)
            .Select(row => LoadoutStats(_conn, row)));
    }
    
    private static readonly Inlet<EntityId> LoadoutId = new();
    
    private static readonly Flow<(EntityId LoadoutId, int ModCount)> ModCount =
        Pattern.Create()
            .Each(LoadoutId, out var loadoutId)
            .Db(out var itemId, LoadoutItem.LoadoutId, loadoutId)
            .Db(itemId, LibraryLinkedLoadoutItem.LibraryItemId, out _)
            .Return(loadoutId, itemId.Count());
    

    private static LoadoutRevisionWithStats LoadoutStats(IConnection conn, LoadoutRevision revision)
    {
        var newDb = conn.AsOf(TxId.From(revision.TxEntity.Value));

        var loadoutInlet = newDb.Topology.Intern(LoadoutId);
        loadoutInlet.Values = [revision.RowId];
        
        var modCount = newDb.Topology.Outlet(ModCount).FirstOrDefault().ModCount;
        return new LoadoutRevisionWithStats(revision, modCount); 
    }
    

    private static readonly IAttribute[] IgnoreAttributes =
    [
        GameInstallMetadata.LastSyncedLoadoutId,
        Loadout.LastAppliedDateTime,
        LoadoutItem.ParentId,
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
        toProcess.Add(revisionRevision.RowId);

        var tx = _conn.BeginTransaction();
        var processed = new HashSet<EntityId>();
        
        var seen = new HashSet<Symbol>();
        
        while (toProcess.Count > 0)
        {
            var current = toProcess.First(); 
            toProcess.Remove(current);
            if (!processed.Add(current))
                continue;

            var desiredState = revertDb.Datoms(current);
            var currentState = currentDb.Datoms(current);


            CompareEntities(current, currentState, processed, toProcess, desiredState, tx, seen, currentDb.AttributeCache);
            ExtractBackReferences(currentDb, current, ignoreAttrs, processed, toProcess, revertDb, seen);
        }

        // Mark the transaction as a snapshot
        tx.Add(EntityId.From(TxId.Tmp.Value), LoadoutSnapshot.Snapshot, revisionRevision.EntityId);

        //await tx.Commit();
    }

    private static void ExtractBackReferences(IDb currentDb, EntityId current, FrozenSet<AttributeId> ignoreAttrs, HashSet<EntityId> processed, HashSet<EntityId> toProcess, IDb revertDb, HashSet<Symbol> seenIds)
    {
        var backReferenceDatoms = currentDb.Datoms(SliceDescriptor.CreateReferenceTo(current));
        var resolver = currentDb.AttributeCache;
        foreach (var datom in backReferenceDatoms)
        {
            if (ignoreAttrs.Contains(datom.A) || processed.Contains(datom.E))
                continue;
            toProcess.Add(datom.E);
            seenIds.Add(resolver.GetSymbol(datom.A));
        }
                
        var backReferenceDesiredState = revertDb.Datoms(SliceDescriptor.CreateReferenceTo(current));
        foreach (var datom in backReferenceDesiredState)
        {
            if (ignoreAttrs.Contains(datom.A) || processed.Contains(datom.E))
                continue;
            toProcess.Add(datom.E);
            seenIds.Add(resolver.GetSymbol(datom.A));
        }
    }

    private static void CompareEntities(EntityId current, EntitySegment currentState, HashSet<EntityId> processed, HashSet<EntityId> toProcess, EntitySegment desiredState, ITransaction tx, HashSet<Symbol> seenIds, AttributeCache cache)
    {
        
        
        
        foreach (var avData in currentState.GetAVEnumerable())
        {
            
            seenIds.Add(cache.GetSymbol(avData.A));
            
            if (avData.ValueType == ValueTag.Reference)
            {
                var value = EntityIdSerializer.Read(avData.Value.Span);
                if (!processed.Contains(value)) 
                    toProcess.Add(value);
            }
                
            if (!desiredState.Contains(avData)) 
                tx.Add(current, avData.A, avData.ValueType, avData.Value.Span, true);
        }

        foreach (var datom in desiredState.GetAVEnumerable())
        {
            
            seenIds.Add(cache.GetSymbol(datom.A));
            
            if (datom.ValueType == ValueTag.Reference)
            {
                var value = EntityIdSerializer.Read(datom.Value.Span);
                if (!processed.Contains(value)) 
                    toProcess.Add(value);
            }
                
            if (!currentState.Contains(datom)) 
                tx.Add(current, datom.A, datom.ValueType, datom.Value.Span);
        }
    }
}
