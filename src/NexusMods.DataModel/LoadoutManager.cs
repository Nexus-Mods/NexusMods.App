using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Sorting;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Loadouts.Synchronizers.Conflicts;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.DataModel.Sorting;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using NexusMods.Sdk.Jobs;

namespace NexusMods.DataModel;

internal partial class LoadoutManager : ILoadoutManager
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IJobMonitor _jobMonitor;
    private readonly IFileHashesService _fileHashesService;
    private readonly IConnection _connection;
    private readonly ISynchronizerService _synchronizerService;
    private readonly IGarbageCollectorRunner _garbageCollectorRunner;

    public LoadoutManager(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<LoadoutManager>>();
        _serviceProvider = serviceProvider;
        _jobMonitor = serviceProvider.GetRequiredService<IJobMonitor>();
        _fileHashesService = serviceProvider.GetRequiredService<IFileHashesService>();
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _synchronizerService = serviceProvider.GetRequiredService<ISynchronizerService>();
        _garbageCollectorRunner = serviceProvider.GetRequiredService<IGarbageCollectorRunner>();
    }

    public IJobTask<CreateLoadoutJob, Loadout.ReadOnly> CreateLoadout(GameInstallation installation, string? suggestedName = null)
    {
        return _jobMonitor.Begin(new CreateLoadoutJob(installation), async ctx =>
        {
            // Prime the hash database to make sure it's loaded
            await _fileHashesService.GetFileHashesDb();

            var shortName = GetNewShortName(_connection.Db, installation.GameMetadataId);

            using var tx = _connection.BeginTransaction();

            List<LocatorId> locatorIds = [];
            if (installation.LocatorResultMetadata != null)
            {
                var metadataLocatorIds = installation.LocatorResultMetadata.ToLocatorIds().ToArray();
                var distinctLocatorIds = metadataLocatorIds.Distinct().ToArray();
                    
                if (distinctLocatorIds.Length != metadataLocatorIds.Length)
                {
                    _logger.LogWarning("Duplicate locator ids `{LocatorIds}` found in LocatorResultMetadata for {Game} when creating new loadout", metadataLocatorIds, installation.Game.DisplayName);
                }

                locatorIds.AddRange(distinctLocatorIds);
            }

            if (!_fileHashesService.TryGetVanityVersion((installation.Store, locatorIds.ToArray()), out var version))
                _logger.LogWarning("Unable to find game version for {Game}", installation.GameMetadataId);

            var loadout = new Loadout.New(tx)
            {
                Name = suggestedName ?? "Loadout " + shortName,
                ShortName = shortName,
                InstallationId = installation.GameMetadataId,
                Revision = 0,
                LoadoutKind = LoadoutKind.Default,
                LocatorIds = locatorIds,
                GameVersion = version,
            };

            // Create the user's default collection
            _ = new CollectionGroup.New(tx, out var userCollectionId)
            {
                IsReadOnly = false,
                LoadoutItemGroup = new LoadoutItemGroup.New(tx, userCollectionId)
                {
                    IsGroup = true,
                    LoadoutItem = new LoadoutItem.New(tx, userCollectionId)
                    {
                        Name = "My Mods",
                        LoadoutId = loadout,
                    },
                },
            };

            var result = await tx.Commit();
            var remappedLoadout = result.Remap(loadout);

            // If there is no currently synced loadout, then we can ingest the game folder
            if (!GameInstallMetadata.LastSyncedLoadout.TryGetValue(remappedLoadout.Installation, out var lastSyncedLoadoutId))
            {
                await _synchronizerService.Synchronize(remappedLoadout.LoadoutId);
                remappedLoadout = remappedLoadout.Rebase();
            }
            else
            {
                // check if the last synced loadout is valid (can apparently happen if the user unmanaged the game and manages it again)
                var lastSyncedLoadout = Loadout.Load(remappedLoadout.Db, lastSyncedLoadoutId);
                if (!lastSyncedLoadout.IsValid())
                {
                    await _synchronizerService.Synchronize(remappedLoadout.LoadoutId);
                    remappedLoadout = remappedLoadout.Rebase();
                }
            }

            return remappedLoadout;
        });
    }

    private static string GetNewShortName(IDb db, GameInstallMetadataId installationId)
    {
        var existingShortNames = Loadout.All(db)
            .Where(l => l.IsVisible() && l.InstallationId == installationId)
            .Select(l => l.ShortName)
            .ToArray();

        var result = LoadoutNameProvider.GetNewShortName(existingShortNames);
        return result;
    }

    public async ValueTask<Loadout.ReadOnly> CopyLoadout(LoadoutId loadoutId, CancellationToken cancellationToken = default)
    {
        var baseDb = _connection.Db;
        var loadout = Loadout.Load(baseDb, loadoutId);

        // Temp space for datom values
        Memory<byte> buffer = GC.AllocateUninitializedArray<byte>(length: 32);

        // Cache some attribute ids
        var cache = baseDb.AttributeCache;
        var nameId = cache.GetAttributeId(Loadout.Name.Id);
        var shortNameId = cache.GetAttributeId(Loadout.ShortName.Id);

        // Create a mapping of old entity ids to new (temp) entity ids
        Dictionary<EntityId, EntityId> entityIdList = new();
        var remapFn = RemapFn;

        using var tx = _connection.BeginTransaction();

        // Add the loadout
        var newLoadoutId = tx.TempId();
        entityIdList[loadout.Id] = newLoadoutId;

        // And each item
        foreach (var item in loadout.Items)
        {
            entityIdList[item.Id] = tx.TempId();
        }

        foreach (var (oldId, newId) in entityIdList)
        {
            // Get the original entity
            var entity = baseDb.Get(oldId);

            foreach (var datom in entity)
            {
                if (datom.A == nameId || datom.A == shortNameId) continue;

                if (buffer.Length < datom.ValueSpan.Length)
                    buffer = GC.AllocateUninitializedArray<byte>(datom.ValueSpan.Length);

                datom.ValueSpan.CopyTo(buffer.Span);

                var prefix = new KeyPrefix(newId, datom.A, TxId.Tmp, isRetract: false, datom.Prefix.ValueTag);
                var newDatom = new Datom(prefix, buffer[..datom.ValueSpan.Length]);

                datom.Prefix.ValueTag.Remap(buffer[..datom.ValueSpan.Length].Span, remapFn);

                tx.Add(newDatom);
            }
        }

        // NOTE(erri120): using latest DB to prevent duplicate short names
        var newShortName = GetNewShortName(_connection.Db, loadout.InstallationId);
        var newName = "Loadout " + newShortName;

        tx.Add(newLoadoutId, Loadout.Name, newName);
        tx.Add(newLoadoutId, Loadout.ShortName, newShortName);

        var result = await tx.Commit();
        var newLoadout = Loadout.Load(result.Db, result[newLoadoutId]);
        return newLoadout;

        // Local function to remap entity ids in the format Attribute.Remap wants
        EntityId RemapFn(EntityId entityId)
        {
            return entityIdList.GetValueOrDefault(entityId, entityId);
        }
    }

    public async ValueTask DeleteLoadout(LoadoutId loadoutId, GarbageCollectorRunMode gcRunMode = GarbageCollectorRunMode.DoNotRun, bool deactivateIfActive = true, CancellationToken cancellationToken = default)
    {
        {
            using var tx1 = _connection.BeginTransaction();
            tx1.Add(loadoutId, Loadout.LoadoutKind, LoadoutKind.Deleted);
            await tx1.Commit();
        }

        var loadout = Loadout.Load(_connection.Db, loadoutId);
        Debug.Assert(!loadout.IsVisible(), "loadout shouldn't be visible anymore");

        var metadata = GameInstallMetadata.Load(_connection.Db, loadout.InstallationInstance.GameMetadataId);
        if (deactivateIfActive && GameInstallMetadata.LastSyncedLoadout.TryGetValue(metadata, out var lastAppliedLoadout) && lastAppliedLoadout == loadoutId.Value)
        {
            await DeactivateCurrentLoadout(loadout.InstallationInstance, cancellationToken: cancellationToken);
        }

        using var tx = _connection.BeginTransaction();

        tx.Delete(loadoutId, recursive: false);
        foreach (var item in loadout.Items)
        {
            tx.Delete(item.Id, recursive: false);
        }

        foreach (var priorityEntity in LoadoutItemGroupPriority.FindByLoadout(loadout.Db, loadout))
        {
            tx.Delete(priorityEntity, recursive: false);
        }

        await tx.Commit();
        
        // Execute the garbage collector
        await _garbageCollectorRunner.RunWithMode(gcRunMode);
    }

    public async ValueTask ActivateLoadout(LoadoutId loadoutId, CancellationToken cancellationToken = default)
    {
        var loadout = Loadout.Load(_connection.Db, loadoutId);
        var synchronizer = loadout.InstallationInstance.GetGame().Synchronizer;

        var state = await synchronizer.ReindexState(loadout.InstallationInstance);
        await synchronizer.BuildProcessRun(loadout, state, cancellationToken);
    }

    public async ValueTask DeactivateCurrentLoadout(GameInstallation installation, CancellationToken cancellationToken = default)
    {
        var metadata = installation.GetMetadata(_connection);
        var synchronizer = installation.GetGame().Synchronizer;

        if (!metadata.Contains(GameInstallMetadata.LastSyncedLoadout)) return;

        // Synchronize the last applied loadout, so we don't lose any changes
        await synchronizer.Synchronize(Loadout.Load(_connection.Db, metadata.LastSyncedLoadout));

        var metadataLocatorIds = installation.LocatorResultMetadata?.ToLocatorIds().ToArray() ?? [];
        var locatorIds = metadataLocatorIds.Distinct().ToArray();
        
        if (locatorIds.Length != metadataLocatorIds.Length)
        {
            _logger.LogWarning("Duplicate locator ids `{LocatorIds}` found in LocatorResultMetadata for {Game} when deactivating loadout", metadataLocatorIds, installation.Game.DisplayName);
        }

        await synchronizer.ResetToOriginalGameState(installation, locatorIds);
    }

    public Optional<LoadoutId> GetCurrentlyActiveLoadout(GameInstallation installation)
    {
        var metadata = installation.GetMetadata(_connection);
        if (!GameInstallMetadata.LastSyncedLoadout.TryGetValue(metadata, out var lastAppliedLoadout))
            return Optional<LoadoutId>.None;
        return LoadoutId.From(lastAppliedLoadout);
    }

    public IJobTask<UnmanageGameJob, GameInstallation> UnManage(GameInstallation installation, bool runGc = true, bool cleanGameFolder = true, CancellationToken cancellationToken = default)
    {
        return _jobMonitor.Begin(new UnmanageGameJob(installation), async ctx =>
        {
            var metadata = installation.GetMetadata(_connection);

            if (GetCurrentlyActiveLoadout(installation).HasValue && cleanGameFolder) await DeactivateCurrentLoadout(installation, cancellationToken: cancellationToken);
            await ctx.YieldAsync();

            {
                using var tx1 = _connection.BeginTransaction();
                foreach (var loadout in metadata.Loadouts)
                {
                    tx1.Add(loadout.Id, Loadout.LoadoutKind, LoadoutKind.Deleted);
                }

                await tx1.Commit();

                metadata = installation.GetMetadata(_connection);
                Debug.Assert(metadata.Loadouts.All(x => !x.IsVisible()), "all loadouts shouldn't be visible anymore");
            }

            foreach (var loadout in metadata.Loadouts)
            {
                _logger.LogInformation("Deleting loadout {Loadout} - {ShortName}", loadout.Name, loadout.ShortName);
                await ctx.YieldAsync();
                await DeleteLoadout(loadout, gcRunMode: GarbageCollectorRunMode.DoNotRun, deactivateIfActive: cleanGameFolder, cancellationToken: cancellationToken);
            }

            // Retract all `GameBakedUpFile` entries to allow for game file backups to be cleaned up from the FileStore
            using var tx = _connection.BeginTransaction();
            foreach (var file in GameBackedUpFile.All(_connection.Db))
            {
                if (file.GameInstallId.Value == installation.GameMetadataId)
                    tx.Delete(file, recursive: false);
            }

            // Delete the last applied/scanned disk state data
            metadata = metadata.Rebase();

            foreach (var entry in metadata.DiskStateEntries)
            {
                tx.Delete(entry, recursive: false);
            }

            if (metadata.Contains(GameInstallMetadata.LastSyncedLoadoutId))
                tx.Retract(metadata, GameInstallMetadata.LastSyncedLoadoutId, metadata.LastSyncedLoadoutId.Value);
            if (metadata.Contains(GameInstallMetadata.LastSyncedLoadoutTransactionId))
                tx.Retract(metadata, GameInstallMetadata.LastSyncedLoadoutTransactionId, metadata.LastSyncedLoadoutTransactionId.Value);
            if (metadata.Contains(GameInstallMetadata.InitialDiskStateTransactionId))
                tx.Retract(metadata, GameInstallMetadata.InitialDiskStateTransactionId, metadata.InitialDiskStateTransactionId.Value);
            if (metadata.Contains(GameInstallMetadata.LastScannedDiskStateTransactionId))
                tx.Retract(metadata, GameInstallMetadata.LastScannedDiskStateTransactionId, metadata.LastScannedDiskStateTransactionId.Value);

            await tx.Commit();

            if (runGc) _garbageCollectorRunner.Run();
            return installation;
        });
    }

    public async Task<LoadoutItemGroup.ReadOnly> InstallItemWrapper(LoadoutId targetLoadout, Func<ITransaction, Task<LoadoutItemGroupId>> func)
    {
        using var tx = _connection.BeginTransaction();

        var groupId = await func(tx);
        tx.Add(new AddPriorityTxFunc(targetLoadout, groupId));

        var result = await tx.Commit();
        return LoadoutItemGroup.Load(result.Db, groupId);
    }

    public IJobTask<IInstallLoadoutItemJob, InstallLoadoutItemJobResult> InstallItem(
        LibraryItem.ReadOnly libraryItem,
        LoadoutId targetLoadout,
        Optional<LoadoutItemGroupId> parent = default,
        ILibraryItemInstaller? installer = null,
        ILibraryItemInstaller? fallbackInstaller = null)
    {
        return InstallItem(libraryItem, targetLoadout, inputTx: null, parent: parent, installer: installer, fallbackInstaller: fallbackInstaller);
    }

    private IJobTask<IInstallLoadoutItemJob, InstallLoadoutItemJobResult> InstallItem(
        LibraryItem.ReadOnly libraryItem,
        LoadoutId targetLoadout,
        ITransaction? inputTx,
        Optional<LoadoutItemGroupId> parent = default,
        ILibraryItemInstaller? installer = null,
        ILibraryItemInstaller? fallbackInstaller = null)
    {
        var mainTransaction = inputTx is null ? _connection.BeginTransaction() : null;
        var tx = inputTx ?? mainTransaction!;

        var job = InstallLoadoutItemJob.Create(_serviceProvider, libraryItem, targetLoadout, tx, groupId: parent, installer: installer, fallbackInstaller: fallbackInstaller);
        return _jobMonitor.Begin(job, async context =>
        {
            var result = await job.StartAsync(context);
            var targetId = result.GroupTxId;
            tx.Add(new AddPriorityTxFunc(targetLoadout, targetId));

            if (mainTransaction is null) return new InstallLoadoutItemJobResult(null, targetId);

            var commitResult = await mainTransaction.Commit();
            var remapped = commitResult[targetId];
            mainTransaction.Dispose();

            return new InstallLoadoutItemJobResult(LoadoutItemGroup.Load(commitResult.Db, remapped), LoadoutItemGroupId.From(0));
        });
    }

    public async ValueTask RemoveItems(LoadoutItemGroupId[] groupIds)
    {
        using var tx = _connection.BeginTransaction();
        RemoveItems(tx, groupIds);
        await tx.Commit();
    }

    private void RemoveItems(ITransaction tx, LoadoutItemGroupId[] groupIds)
    {
        var db = _connection.Db;
        var loadouts = new Dictionary<LoadoutId, List<LoadoutItemGroupId>>();

        foreach (var groupId in groupIds)
        {
            var group = LoadoutItemGroup.Load(db, groupId);
            if (!group.IsValid()) continue;

            tx.Delete(groupId, recursive: true);

            var priorities = LoadoutItemGroupPriority.FindByTarget(db, groupId);
            foreach (var priority in priorities)
            {
                tx.Delete(priority.Id, recursive: false);
            }

            var loadoutId = group.AsLoadoutItem().LoadoutId;
            if (!loadouts.TryGetValue(loadoutId, out var list))
            {
                list = new List<LoadoutItemGroupId>(capacity: groupIds.Length);
                loadouts[loadoutId] = list;
            }

            list.Add(groupId);
        }

        foreach (var kv in loadouts)
        {
            tx.Add(new RebalancePrioritiesTxFunc(kv.Key, kv.Value.Select(x => x.Value).ToArray()));
        }
    }

    public async ValueTask RemoveCollection(CollectionGroupId collection)
    {
        var db = _connection.Db;
        using var tx = _connection.BeginTransaction();

        tx.Delete(collection, recursive: true);
        var groupIds = LoadoutItem.FindByParent(db, collection).OfTypeLoadoutItemGroup().Select(x => x.LoadoutItemGroupId).ToArray();
        RemoveItems(tx, groupIds);

        await tx.Commit();
    }

    public async ValueTask<CollectionGroup.ReadOnly> CloneCollection(CollectionGroupId collectionId)
    {
        Span<byte> refScratch = stackalloc byte[8];
        using var writer = new PooledMemoryBufferWriter();

        var basisDb = _connection.Db;

        Dictionary<EntityId, EntityId> remappedIds = new();
        var query = _connection.Query<EntityId>($"""
                                          WITH RECURSIVE ChildLoadoutItems (Id) AS 
                                          (SELECT {collectionId.Value} 
                                          UNION
                                          SELECT Id FROM (SELECT Id, Parent FROM mdb_LoadoutItem(Db=>{basisDb})
                                          UNION ALL
                                          SELECT Id, ParentEntity FROM mdb_SortOrder(Db=>{basisDb})
                                          UNION ALL
                                          SELECT Id, ParentSortOrder FROM mdb_SortOrderItem(Db=>{basisDb}))
                                          WHERE Parent in (SELECT Id FROM ChildLoadoutItems))
                                          SELECT DISTINCT Id FROM ChildLoadoutItems
                                          """);
        using var tx = _connection.BeginTransaction();
        foreach (var itemId in query)
        {
            remappedIds.TryAdd(itemId, tx.TempId());
        }

        foreach (var (oldId, newId) in remappedIds)
        {
            var entity = basisDb.Get(oldId);
            foreach (var datom in entity)
            {
                // Remap the value part of references
                if (datom.Prefix.ValueTag == ValueTag.Reference)
                {
                    var oldRef = EntityId.From(UInt64Serializer.Read(datom.ValueSpan));
                    if (!remappedIds.TryGetValue(oldRef, out var newRef))
                    {
                        tx.Add(newId, datom.A, datom.Prefix.ValueTag, datom.ValueSpan);
                        continue;
                    }
                    MemoryMarshal.Write(refScratch, newRef);
                    tx.Add(newId, datom.A, datom.Prefix.ValueTag, refScratch);
                }
                // It's rare, but the Ref,UShort/String tuple type may include a ref that needs to be remapped
                else if (datom.Prefix.ValueTag == ValueTag.Tuple3_Ref_UShort_Utf8I)
                {
                    var (r, s, str) = Tuple3_Ref_UShort_Utf8I_Serializer.Read(datom.ValueSpan);
                    if (!remappedIds.TryGetValue(r, out var newR))
                    {
                        tx.Add(newId, datom.A, datom.Prefix.ValueTag, datom.ValueSpan);
                        continue;
                    }
                    writer.Reset();
                    var newTuple = (newR, s, str);
                    Tuple3_Ref_UShort_Utf8I_Serializer.Write(newTuple, writer);
                    tx.Add(newId, datom.A, datom.Prefix.ValueTag, writer.GetWrittenSpan());
                }
                // Otherwise just remap the E value
                else
                {
                    tx.Add(newId, datom.A, datom.Prefix.ValueTag, datom.ValueSpan);
                }
            }
        }

        var loadoutId = CollectionGroup.Load(basisDb, collectionId).AsLoadoutItemGroup().AsLoadoutItem().LoadoutId;
        var nextPriority = GetNextPriority(loadoutId, _connection.Db);
        var oldPriorities = LoadoutItemGroupPriority.All(basisDb).Where(priority => priority.Target.AsLoadoutItem().ParentId.Value == collectionId.Value).OrderBy(x => x.Priority).ToArray();

        for (uint i = 0; i < oldPriorities.Length; i++)
        {
            var oldPriority = oldPriorities[i];
            _ = new LoadoutItemGroupPriority.New(tx)
            {
                LoadoutId = loadoutId,
                Priority = ConflictPriority.From(nextPriority.Value + i),
                TargetId = remappedIds[oldPriority.TargetId],
            };
        }

        tx.Add(new RebalancePrioritiesTxFunc(loadoutId, toSkip: []));

        var result = await tx.Commit();
        return CollectionGroup.Load(result.Db, result[remappedIds[collectionId]]);
    }

    public async ValueTask ReplaceItems(LoadoutId loadoutId, LoadoutItemGroupId[] groupsToRemove, LibraryItem.ReadOnly libraryItemToInstall)
    {
        using var tx = _connection.BeginTransaction();
        RemoveItems(tx, groupsToRemove);
        await InstallItem(libraryItemToInstall, loadoutId, inputTx: tx);
        await tx.Commit();
    }

    public async ValueTask ResolveFileConflicts(LoadoutItemGroupPriorityId[] winnerIds, LoadoutItemGroupPriorityId loserId)
    {
        var db = _connection.Db;
        using var tx = _connection.BeginTransaction();

        tx.Add(new ResolveFileConflictsTxFunc(
            loadoutId: LoadoutItemGroupPriority.Load(db,winnerIds[0]).LoadoutId,
            winnerIds: winnerIds,
            loserId: loserId
        ));

        await tx.Commit();
    }

    public async ValueTask ApplyCollectionDownloadRules(NexusCollectionLoadoutGroupId collectionId)
    {
        var items = _connection.Query<EntityId>($"SELECT Id FROM MDB_NexusCollectionItemLoadoutGroup(Db => {_connection}) WHERE Parent = {collectionId.Value}").ToList();

        var rulesQuery = _connection.Query<(EntityId SourceId, EntityId OtherId, int RuleType)>(
            $"""
              SELECT Source, Other, RuleType FROM loadouts.CollectionRulesOnItems({_connection})
              WHERE Parent = {collectionId.Value};
              """
        );

        var rules = rulesQuery
            .GroupBy(tuple => tuple.SourceId, tuple => (tuple.OtherId, tuple.RuleType))
            .ToFrozenDictionary(group => group.Key, group => group.ToArray());

        var sortedItems = new Sorter().Sort(
            items: items,
            idSelector: static x => x,
            ruleFn: id =>
            {
                if (!rules.TryGetValue(id, out var itemRules)) return [];
                return itemRules.Select(ISortRule<EntityId, EntityId> (rule) => (CollectionDownloadRuleType)rule.RuleType switch
                {
                    CollectionDownloadRuleType.Before => new Before<EntityId, EntityId>() { Other = rule.OtherId },
                    CollectionDownloadRuleType.After => new After<EntityId, EntityId>() { Other = rule.OtherId },
                    _ => throw new UnreachableException(),
                }).ToArray();
            }
        ).ToImmutableArray();

        using var tx = _connection.BeginTransaction();

        var loadoutId = LoadoutItem.Load(_connection.Db, collectionId).LoadoutId;
        tx.Add(new ApplyCollectionRulesTxFunc(loadoutId, sortedItems));
        await tx.Commit();
    }

    public async ValueTask LoseAllFileConflicts(LoadoutItemGroupPriorityId[] loserIds)
    {
        var db = _connection.Db;
        using var tx = _connection.BeginTransaction();

        tx.Add(new ResolveFileConflictsTxFunc(
            loadoutId: LoadoutItemGroupPriority.Load(db, loserIds[0]).LoadoutId,
            winnerIds: loserIds,
            loserId: Optional<LoadoutItemGroupPriorityId>.None
        ));

        await tx.Commit();
    }

    public ValueTask WinAllFileConflicts(LoadoutItemGroupPriorityId[] winnerIds)
    {
        var db = _connection.Db;
        var loser = LoadoutItemGroupPriority
            .FindByLoadout(db, LoadoutItemGroupPriority.Load(db, winnerIds[0]).LoadoutId)
            .OrderByDescending(static model => model.Priority)
            .First();

        return ResolveFileConflicts(winnerIds: winnerIds, loserId: loser.LoadoutItemGroupPriorityId);
    }
}
