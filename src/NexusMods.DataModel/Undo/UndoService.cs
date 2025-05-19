using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Threading.Channels;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Cascade;
using NexusMods.Cascade.Patterns;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.Cascade;
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
    private readonly IFileStore _fileStore;
    private static readonly Inlet<IDb> _gameDb = new();

    public readonly Flow<LoadoutRevisionWithStats> Revisions;

    /// <summary>
    /// DI constructor 
    /// </summary>
    /// <param name="connection"></param>
    public UndoService(IConnection connection, IFileStore fileStore)
    {
        _conn = connection;
        _fileStore = fileStore;
        Revisions = Queries.LoadoutRevisionsWithMetadata.ParallelSelect(LoadoutStats);
    }
    
    /// <summary>
    /// This query isn't super efficient, but for every stat loadout we have to load one or (in the future) two
    /// databases. So it's O(n) for the number of revisions for now. We can optimize it in the future
    /// </summary>
    private LoadoutRevisionWithStats LoadoutStats(LoadoutRevision revision)
    {
        var currentDb = _conn.AsOf(TxId.From(revision.TxEntity.Value));
        var prevDb = _conn.AsOf(TxId.From(revision.PrevTxEntity.Value));

        var txEntity = Transaction.Load(currentDb, revision.TxEntity);

        var currentLoadout = Loadout.Load(currentDb, revision.EntityId);
        var prevLoadout = Loadout.Load(prevDb, revision.EntityId);

        var synchronizer = currentLoadout.InstallationInstance.GetGame().Synchronizer;
        
        var flattenedCurrent = synchronizer.Flatten(currentLoadout);
        Dictionary<GamePath, SyncNode> flattenedPrev = new();
        if (prevLoadout.IsValid()) 
            flattenedPrev = synchronizer.Flatten(prevLoadout);

        var added = 0;
        var removed = 0;
        var modified = 0;
        var missingBackup = 0;

        foreach (var (key, value) in flattenedCurrent)
        {
            // Ignore any files that are not part of the loadout (but may exist on disk)
            if (!value.HaveLoadout)
                continue;

            // If it's a game file, and we'd need to write the file (what's on disk is different), increment the missing count 
            // if we don't have a source for the file
            if (value.SourceItemType == LoadoutSourceItemType.Game && 
                !(value.HaveDisk || value.Disk.Hash != value.Loadout.Hash) && 
                !_fileStore.HaveFile(value.Loadout.Hash))
            {
                missingBackup++;
            }
                
            
            if (flattenedPrev.TryGetValue(key, out var prevValue) && prevValue.HaveLoadout)
            {
                if (prevValue.Loadout.Hash != value.Loadout.Hash)
                {
                    modified++;
                }
            }
            else
            {
                added++;
            }
        }
        
        foreach (var (key, _) in flattenedPrev)
        {
            if (flattenedCurrent.TryGetValue(key, out var currentValue) && currentValue.HaveLoadout)
                continue;
            removed++;
        }
        
        return new LoadoutRevisionWithStats
        {
            LoadoutId = revision.EntityId,
            TxId = revision.TxEntity,
            Added = added,
            Removed = removed,
            Modified = modified,
            MissingGameFiles = missingBackup,
            Timestamp = txEntity.Timestamp,
        };
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
    ];

    /// <summary>
    /// Reverts the given loadout to the given revision.
    /// </summary>
    public async Task RevertTo(EntityId loadout, TxId asOfId)
    {
        var currentDb = _conn.Db;
        var revertDb = _conn.AsOf(TxId.From(asOfId.Value));
        var ignoreAttrs = IgnoreAttributes.Select(a => currentDb.AttributeCache.GetAttributeId(a.Id)).ToFrozenSet();
        var toProcess = new ConcurrentBag<EntityId>();
        toProcess.Add(loadout);

        var tx = _conn.BeginTransaction();
        var processed = new ConcurrentDictionary<EntityId, EntityId>();
        //var toProcess = Channel.CreateUnbounded<EntityId>();

        int taskCount = Environment.ProcessorCount;
        while (toProcess.Count > 0)
        {
            var processList = toProcess.ToList();
            toProcess.Clear();

            var tasks = new Task[taskCount];
            for (int i = 0; i < taskCount; i++)
            {
                var i1 = i;
                tasks[i] = Task.Run(() =>
                    {
                        for (var idx = i1; idx < processList.Count; idx += taskCount)
                        {
                            var current = processList[idx];
                            // Skip if the entity has already been processed
                            if (!processed.TryAdd(current, current))
                                continue;

                            // Skip if the entity is a transaction entity
                            if (current.Partition == PartitionId.Transactions)
                                continue;

                            var desiredState = revertDb.Datoms(current);
                            var currentState = currentDb.Datoms(current);

                            CompareEntities(current, currentState, processed,
                                toProcess, desiredState, tx,
                                ignoreAttrs
                            );
                            ExtractBackReferences(currentDb, current, ignoreAttrs,
                                processed, toProcess, revertDb
                            );
                        }
                    }
                );
            }
            await Task.WhenAll(tasks);
        }
    }

    private static void ExtractBackReferences(IDb currentDb, EntityId current, FrozenSet<AttributeId> ignoreAttrs, ConcurrentDictionary<EntityId, EntityId> processed, ConcurrentBag<EntityId> toProcess, IDb revertDb)
    {
        var backReferenceDatoms = currentDb.Datoms(SliceDescriptor.CreateReferenceTo(current));
        var resolver = currentDb.AttributeCache;
        foreach (var datom in backReferenceDatoms)
        {
            if (ignoreAttrs.Contains(datom.A) || processed.ContainsKey(datom.E) || datom.E.Partition == PartitionId.Transactions)
                continue;
            
            toProcess.Add(datom.E);
        }
                
        var backReferenceDesiredState = revertDb.Datoms(SliceDescriptor.CreateReferenceTo(current));
        foreach (var datom in backReferenceDesiredState)
        {
            if (ignoreAttrs.Contains(datom.A) || processed.ContainsKey(datom.E)  || datom.E.Partition == PartitionId.Transactions)
                continue;
            toProcess.Add(datom.E);
        }
    }

    private static void CompareEntities(EntityId current, EntitySegment currentState, ConcurrentDictionary<EntityId, EntityId> processed, ConcurrentBag<EntityId> toProcess, EntitySegment desiredState, ITransaction tx, FrozenSet<AttributeId> ignoreAttrs)
    {
        
        foreach (var avData in currentState.GetAVEnumerable())
        {
            if (ignoreAttrs.Contains(avData.A))
                continue;
            
            if (avData.ValueType == ValueTag.Reference)
            {
                var value = EntityIdSerializer.Read(avData.Value.Span);
                if (!processed.ContainsKey(value) && value.Partition != PartitionId.Transactions)
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
                if (!processed.ContainsKey(value) && value.Partition != PartitionId.Transactions) 
                    toProcess.Add(value);
            }
                
            if (!currentState.Contains(datom)) 
                tx.Add(current, datom.A, datom.ValueType, datom.Value.Span);
        }
    }
}
