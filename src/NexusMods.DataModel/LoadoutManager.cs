using System.Diagnostics;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Loadouts.Synchronizers.Conflicts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
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
                    _logger.LogWarning("Duplicate locator ids `{LocatorIds}` found in LocatorResultMetadata for {Game} when creating new loadout", metadataLocatorIds, installation.Game.Name);
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

        var state = await synchronizer.ReindexState(loadout.InstallationInstance, ignoreModifiedDates: false);
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
            _logger.LogWarning("Duplicate locator ids `{LocatorIds}` found in LocatorResultMetadata for {Game} when deactivating loadout", metadataLocatorIds, installation.Game.Name);
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

    public IJobTask<IInstallLoadoutItemJob, InstallLoadoutItemJobResult> InstallItem(
        LibraryItem.ReadOnly libraryItem,
        LoadoutId targetLoadout,
        Optional<LoadoutItemGroupId> parent = default,
        ILibraryItemInstaller? installer = null,
        ILibraryItemInstaller? fallbackInstaller = null,
        ITransaction? transaction = null)
    {
        IMainTransaction? mainTransaction;
        ITransaction tx;

        if (transaction is null)
        {
            mainTransaction = _connection.BeginTransaction();
            tx = mainTransaction;
        }
        else
        {
            mainTransaction = null;
            tx = transaction;
        }

        var job = InstallLoadoutItemJob.Create(_serviceProvider, libraryItem, targetLoadout, tx, groupId: parent, installer: installer, fallbackInstaller: fallbackInstaller);
        return _jobMonitor.Begin(job, async context =>
        {
            var result = await job.StartAsync(context);
            var targetId = result.GroupTxId;
            tx.Add(new AddPriorityTxFunc(targetLoadout, targetId));

            if (mainTransaction is null) return result;

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

    public void RemoveItems(ITransaction tx, LoadoutItemGroupId[] groupIds)
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

    public async ValueTask RemoveCollection(LoadoutId loadoutId, CollectionGroupId collection)
    {
        var db = _connection.Db;
        using var tx = _connection.BeginTransaction();

        tx.Delete(collection, recursive: true);
        var groupIds = LoadoutItem.FindByParent(db, collection).OfTypeLoadoutItemGroup().Select(x => x.LoadoutItemGroupId).ToArray();
        RemoveItems(tx, groupIds);

        await tx.Commit();
    }
}
