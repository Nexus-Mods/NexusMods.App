using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance.Buffers;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.Games.FileHashes.Models;
using NexusMods.Abstractions.GC;
using NexusMods.Sdk.Hashes;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.Loadouts.Files.Diff;
using NexusMods.Abstractions.Loadouts.Sorting;
using NexusMods.Abstractions.Loadouts.Synchronizers.Rules;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Hashing.xxHash3;
using NexusMods.Hashing.xxHash3.Paths;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.FileStore;
using NexusMods.Sdk.IO;
using ReactiveUI;
using OneOf;
using Reloaded.Memory.Extensions;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

using DiskState = Entities<DiskStateEntry.ReadOnly>;

/// <summary>
/// Base class for loadout synchronizers, provides some common functionality. Does not have to be user,
/// but reduces a lot of boilerplate, and is highly recommended.
/// </summary>
[PublicAPI]
public class ALoadoutSynchronizer : ILoadoutSynchronizer
{
    /// <summary>
    /// We'll limit backups to 2GB, for now we should never see much more than this
    /// of modified game files. s
    /// </summary>
    private static Size MaximumBackupSize => Size.GB * 2;
    
    private readonly ScopedAsyncLock _lock = new();
    private readonly IFileStore _fileStore;

    protected readonly ILogger Logger;
    private readonly IOSInformation _os;
    private readonly ISorter _sorter;
    private readonly IGarbageCollectorRunner _garbageCollectorRunner;
    private readonly ISynchronizerService _synchronizerService;
    private readonly IServiceProvider _serviceProvider;
    
    private readonly StringPool _fileNamePool = new();


    /// <summary>
    /// Connection.
    /// </summary>
    protected readonly IConnection Connection;

    private readonly IJobMonitor _jobMonitor;
    private readonly IFileHashesService _fileHashService;

    /// <summary>
    /// Loadout synchronizer base constructor.
    /// </summary>
    protected ALoadoutSynchronizer(
        IServiceProvider serviceProvider,
        ILogger logger,
        IFileStore fileStore,
        ISorter sorter,
        IConnection conn,
        IOSInformation os,
        IFileHashesService fileHashService,
        IGarbageCollectorRunner garbageCollectorRunner)
    {
        _serviceProvider = serviceProvider;
        _synchronizerService = serviceProvider.GetRequiredService<ISynchronizerService>();
        _jobMonitor = serviceProvider.GetRequiredService<IJobMonitor>();
        
        _fileHashService = fileHashService;

        Logger = logger;
        _fileStore = fileStore;
        _sorter = sorter;
        Connection = conn;
        _os = os;
        _garbageCollectorRunner = garbageCollectorRunner;
    }

    /// <summary>
    /// Helper constructor that takes only a service provider, and resolves the dependencies from it.
    /// </summary>
    /// <param name="provider"></param>
    protected ALoadoutSynchronizer(IServiceProvider provider) : this(
        provider,
        provider.GetRequiredService<ILogger<ALoadoutSynchronizer>>(),
        provider.GetRequiredService<IFileStore>(),
        provider.GetRequiredService<ISorter>(),
        provider.GetRequiredService<IConnection>(),
        provider.GetRequiredService<IOSInformation>(),
        provider.GetRequiredService<IFileHashesService>(),
        provider.GetRequiredService<IGarbageCollectorRunner>()
    ) { }

    private void CleanDirectories(IEnumerable<GamePath> directoriesWithDeletions, DiskState newDiskState, GameInstallation installation)
    {
        var processedDirectories = new HashSet<GamePath>();
        var directoriesToDelete = new HashSet<GamePath>();
        var directoriesInUse = new HashSet<GamePath>();
        
        // Build set of directories that are in use (are ancestors of at least one file)
        foreach (var fileEntry in newDiskState)
        {
            var path = (GamePath)fileEntry.Path;
            var parent = path.Parent;
            var rootComponent = parent.GetRootComponent;
        
            // Add all parent directories to the set
            while (parent != rootComponent)
            {
                directoriesInUse.Add(parent);
                parent = parent.Parent;
            }
        }
        
        // Find the highest directory not in use for each deletion 
        foreach (var dirWithDeletion in directoriesWithDeletions)
        {
            var rootComponent = dirWithDeletion.GetRootComponent;
            GamePath? highestEmptyDirectory = null;
            
            var currentParentDir = dirWithDeletion;

            while (currentParentDir != rootComponent)
            {
                if (processedDirectories.Contains(currentParentDir))
                {
                    highestEmptyDirectory = null;
                    break;
                }
                
                // Check if directory contains files or is a parent of directories with files
                if (directoriesInUse.Contains(currentParentDir))
                {
                    break;
                }

                processedDirectories.Add(currentParentDir);
                highestEmptyDirectory = currentParentDir;
                currentParentDir = currentParentDir.Parent;
            }

            if (highestEmptyDirectory != null)
                directoriesToDelete.Add(highestEmptyDirectory.Value);
        }

        foreach (var dir in directoriesToDelete)
        {
            // Could have other empty directories as children, so we need to delete recursively
            installation.LocationsRegister.GetResolvedPath(dir).DeleteDirectory(recursive: true);
        }
    }

#region ILoadoutSynchronizer Implementation

    /// <summary>
    /// Gets or creates the override group.
    /// </summary>
    protected LoadoutOverridesGroupId GetOrCreateOverridesGroup(ITransaction tx, Loadout.ReadOnly loadout)
    {
        if (LoadoutOverridesGroup.FindByOverridesFor(loadout.Db, loadout.Id).TryGetFirst(out var found))
            return found;

        var newOverrides = new LoadoutOverridesGroup.New(tx, out var id)
        {
            OverridesForId = loadout,
            LoadoutItemGroup = new LoadoutItemGroup.New(tx, id)
            {
                IsGroup = true,
                LoadoutItem = new LoadoutItem.New(tx, id)
                {
                    Name = "Overrides",
                    LoadoutId = loadout.Id,
                },
            },
        };

        return newOverrides.Id;
    }

    public Dictionary<GamePath, FileConflictGroup> GetFileConflicts(Loadout.ReadOnly loadout, bool removeDuplicates = true)
    {
        var db = loadout.Db;
        var query = Loadout.FileConflictsQuery(db, loadout, removeDuplicates: removeDuplicates);
        var result = query.ToDictionary(row => new GamePath(row.Location, row.Path), row =>
        {
            var items = row.Item3.Select(tuple =>
            {
                var (entityId, isEnabled, isDeleted) = tuple;
                OneOf<LoadoutFile.ReadOnly, DeletedFile.ReadOnly> file = isDeleted ? DeletedFile.Load(db, entityId) : LoadoutFile.Load(db, entityId);
                return new FileConflictItem(isEnabled, file);
            }).ToArray();

            return new FileConflictGroup(new GamePath(row.Location, row.Path), items);
        });

        return result;
    }

    public Dictionary<LoadoutItemGroup.ReadOnly, LoadoutFile.ReadOnly[]> GetFileConflictsByParentGroup(Loadout.ReadOnly loadout, bool removeDuplicates = true)
    {
        var db = loadout.Db;
        var query = Loadout.FileConflictsByParentGroupQuery(db, loadout, removeDuplicates: removeDuplicates);
        var result = query.ToDictionary(row => LoadoutItemGroup.Load(db,row.GroupId), row =>
        {
            var items = row.Item2.Select(tuple =>
            {
                var (entityId, locationId, targetPath) = tuple;
                var file = LoadoutFile.Load(db, entityId);
                return file;
            }).ToArray();

            return items;
        }, comparer: LoadoutItemGroupComparer.Instance);

        return result;
    }

    private class LoadoutItemGroupComparer : IEqualityComparer<LoadoutItemGroup.ReadOnly>, IAlternateEqualityComparer<EntityId, LoadoutItemGroup.ReadOnly>
    {
        public static readonly IEqualityComparer<LoadoutItemGroup.ReadOnly> Instance = new LoadoutItemGroupComparer();

        public bool Equals(LoadoutItemGroup.ReadOnly x, LoadoutItemGroup.ReadOnly y) => x.Id.Equals(y.Id);
        public int GetHashCode(LoadoutItemGroup.ReadOnly item) => item.Id.GetHashCode();
        public bool Equals(EntityId alternate, LoadoutItemGroup.ReadOnly other) => other.Id.Equals(alternate);
        public int GetHashCode(EntityId alternate) => alternate.GetHashCode();
        public LoadoutItemGroup.ReadOnly Create(EntityId alternate) => throw new NotSupportedException();
    }
    
    public Dictionary<GamePath, SyncNode> BuildSyncTree<T>(T latestDiskState, T previousDiskState, Loadout.ReadOnly loadout) where T : IEnumerable<PathPartPair>
    {
        var referenceDb = _fileHashService.Current;
        Dictionary<GamePath, SyncNode> syncTree = new();

        // Add in the game state
        foreach (var gameFile in GetNormalGameState(referenceDb, loadout))
        {
            ref var syncTreeEntry = ref CollectionsMarshal.GetValueRefOrAddDefault(syncTree, gameFile.Path, out var exists);
            
            // NOTE(Al12rs): DLCs could have replacements for base game files, but we don't currently store the LocatorId order data so for now we just log cases to be aware of them.
            // See https://partner.steamgames.com/doc/store/application/depots#depot_mounting_rules for steam example
            if (exists)
            {
                Logger.LogWarning("Found duplicate game file `{Path}` in Loadout {LoadoutName} for game {Game}", gameFile.Path, loadout.Name, loadout.InstallationInstance.Game.Name);
            }
            
            // If the entry already exists, we replace it
            syncTreeEntry = new SyncNode
            {
                Loadout = new SyncNodePart
                {
                    Hash = gameFile.Hash,
                    Size = gameFile.Size,
                    LastModifiedTicks = 0,
                },
                SourceItemType = LoadoutSourceItemType.Game,
            };
        }

        foreach (var loadoutItem in Loadout.LoadoutFileMetadataQuery(loadout.Db, loadout.Id, onlyEnabled: true))
        {
            if (loadoutItem.Path.Path == null)
                throw new InvalidOperationException("Path is null");
            var targetPath = new GamePath(loadoutItem.Location, loadoutItem.Path);

            SyncNodePart sourceItem;
            LoadoutSourceItemType sourceItemType;
            if (!loadoutItem.IsDeleted)
            {
                sourceItem = new SyncNodePart
                {
                    Size = loadoutItem.Size,
                    Hash = loadoutItem.Hash,
                    EntityId = loadoutItem.Id,
                    LastModifiedTicks = 0,
                };
                sourceItemType = LoadoutSourceItemType.Loadout;
            }
            else if (loadoutItem.IsDeleted)
            {
                sourceItem = new SyncNodePart
                {
                    Size = Size.Zero,
                    Hash = Hash.Zero,
                    EntityId = loadoutItem.Id,
                    LastModifiedTicks = 0,
                };
                sourceItemType = LoadoutSourceItemType.Deleted;
            }
            else
            {
                throw new NotSupportedException("Only files and deleted files are supported");
            }
            
            ref var existing = ref CollectionsMarshal.GetValueRefOrAddDefault(syncTree, targetPath, out var exists);
            if (!exists)
            {
                existing = new SyncNode
                {
                    Loadout = sourceItem,
                    SourceItemType = sourceItemType,
                };
            }
            else
            {
                if (ShouldWinWrapper(loadout.Db, targetPath, existing.Loadout, existing.SourceItemType, sourceItem, sourceItemType))
                {
                    existing.Loadout = sourceItem;
                    existing.SourceItemType = sourceItemType;
                }
            }
        }

        // Add in the intrinsic files
        foreach (var file in IntrinsicFiles(loadout).Values)
        {
            ref var found = ref CollectionsMarshal.GetValueRefOrAddDefault(syncTree, file.Path, out var exists);
            if (exists)
            {
                Logger.LogWarning("Found duplicate intrinsic file `{Path}` in Loadout {LoadoutName} for game {Game}", file.Path, loadout.Name, loadout.InstallationInstance.Game.Name);
            }
            found.SourceItemType = LoadoutSourceItemType.Intrinsic;
            var stream = new MemoryStream();
            file.Write(stream, loadout, syncTree);
            stream.Position = 0;
            var span = stream.GetBuffer().AsSpan(0, (int)stream.Length);
            found.Loadout = new SyncNodePart()
            {
                Hash = span.xxHash3(),
                Size = Size.From((ulong)span.Length),
                LastModifiedTicks = 0,
            };
            found.SourceItemType = LoadoutSourceItemType.Intrinsic;
        }
        
        // Remove deleted files. I'm not super happy with this requiring a full scan of
        // the loadout, but we have to somehow mark the deleted files and then delete them. 
        // And we can't modify the dictionary while iterating over it.
        List<GamePath> deletedFiles = [];
        foreach (var (key, value) in syncTree)
        {
            if (value.SourceItemType == LoadoutSourceItemType.Deleted) 
                deletedFiles.Add(key);
        }
        foreach (var file in deletedFiles)
        {
            syncTree.Remove(file);
        }
        
        MergeStates(latestDiskState, previousDiskState, syncTree);
        return syncTree;
    }

    public IEnumerable<LoadoutSourceItem> GetNormalGameState(IDb referenceDb, Loadout.ReadOnly loadout)
    {
        var locatorIds = loadout.LocatorIds.Distinct().ToArray();
        if (locatorIds.Length != loadout.LocatorIds.Count)
            Logger.LogWarning("Found duplicate locator IDs `{LocatorIds}` on Loadout {Name} for game `{Game}` when getting game state", loadout.LocatorIds, loadout.Name, loadout.InstallationInstance.Game.Name);
        
        foreach (var item in _fileHashService.GetGameFiles((loadout.InstallationInstance.Store, locatorIds)))
        {
            yield return new LoadoutSourceItem
            {
                Path = item.Path,
                Size = item.Size,
                Hash = item.Hash,
                Type = LoadoutSourceItemType.Game,
            };
        }
    }

    /// <inheritdoc />
    public void MergeStates(IEnumerable<PathPartPair> currentState, IEnumerable<PathPartPair> previousTree, Dictionary<GamePath, SyncNode> loadoutItems)
    {
        foreach (var node in previousTree)
        {
            ref var existing = ref CollectionsMarshal.GetValueRefOrAddDefault(loadoutItems, node.Path, out var exists);
            if (exists)
            {
                existing.Previous = node.Part;
            }
            else
            {
                existing = new SyncNode
                {
                    Previous = node.Part,
                };
            }
        }
        
        foreach (var node in currentState)
        {
            ref var existing = ref CollectionsMarshal.GetValueRefOrAddDefault(loadoutItems, node.Path, out var exists);
            if (exists)
            {
                existing.Disk = node.Part;
            }
            else
            {
                existing = new SyncNode
                {
                    Disk = node.Part,
                };
            }
        }
    }

    /// <summary>
    /// Returns true if the file and all its parents are not disabled.
    /// </summary>
    private static bool FileIsEnabled(LoadoutItem.ReadOnly arg)
    {
        return !arg.GetThisAndParents().Any(f => f.Contains(LoadoutItem.Disabled));
    }

    /// <inheritdoc />
    public async Task<Dictionary<GamePath, SyncNode>> BuildSyncTree(Loadout.ReadOnly loadout)
    {
        var metadata = await ReindexState(loadout.InstallationInstance, ignoreModifiedDates: false, Connection);

        var currentItems = GetDiskStateForGame(metadata);
        var prevItems = ((ILoadoutSynchronizer)this).GetPreviouslyAppliedDiskState(metadata);
        
        return BuildSyncTree(currentItems, prevItems, loadout);
    }


    
    /// <summary>
    /// This is a highly optimized way to load all the disk state for a game. It's a sorted merge
    /// join over all the required attributes for the results
    /// </summary>
    public unsafe List<PathPartPair> GetDiskStateForGame(GameInstallMetadata.ReadOnly metadata)
    {
        var db = metadata.Db;
        var pairs = new List<PathPartPair>();
        var mainAttrId = db.AttributeCache.GetAttributeId(DiskStateEntry.GameId.Id);
        var pathAttrId = db.AttributeCache.GetAttributeId(DiskStateEntry.Path.Id);
        var hashAttrId = db.AttributeCache.GetAttributeId(DiskStateEntry.Hash.Id);
        var sizeAttrId = db.AttributeCache.GetAttributeId(DiskStateEntry.Size.Id);
        var lastModifiedAttrId = db.AttributeCache.GetAttributeId(DiskStateEntry.LastModified.Id);
        
        // We start with a single reference iterator, that points to the game data we are trying to access
        // Since this data will return results sorted by E (entry Id) we can merge join to any other data 
        // that is sorted in the same order
        using var iterator = db.LightweightDatoms(SliceDescriptor.Create(mainAttrId, metadata));
        
        // Now we have iterators for each field to load
        using var pathIterator = db.LightweightDatoms(SliceDescriptor.Create(pathAttrId));
        using var hashIterator = db.LightweightDatoms(SliceDescriptor.Create(hashAttrId));
        using var sizeIterator = db.LightweightDatoms(SliceDescriptor.Create(sizeAttrId));
        using var lastModifiedIterator = db.LightweightDatoms(SliceDescriptor.Create(lastModifiedAttrId));
        
        // For each entry in the main iterator
        while (iterator.MoveNext())
        {
            // Fast-forward the other iterators to the same entry
            pathIterator.FastForwardTo(iterator.KeyPrefix.E);
            hashIterator.FastForwardTo(iterator.KeyPrefix.E);
            sizeIterator.FastForwardTo(iterator.KeyPrefix.E);
            lastModifiedIterator.FastForwardTo(iterator.KeyPrefix.E);
            
            // Get the location id for the path
            var locationId = MemoryMarshal.Read<LocationId>(pathIterator.ValueSpan.SliceFast(sizeof(EntityId)));
            var pathSpan = pathIterator.ValueSpan.SliceFast(sizeof(EntityId) + sizeof(LocationId));
            // The number of paths in a loadout don't often change much, so we'll put them all through a cache pool, which will
            // allow us to not have to create UTF16 strings on every load of the data
            var pathStr = _fileNamePool.GetOrAdd(pathSpan, Encoding.UTF8);
            var gamePath = new GamePath(locationId, RelativePath.CreateUnsafe(pathStr));

            var pathPartPair = new PathPartPair
            {
                Path = gamePath,
                Part = new SyncNodePart
                {
                    EntityId = iterator.KeyPrefix.E,
                    Hash = MemoryMarshal.Read<Hash>(hashIterator.ValueSpan),
                    Size = MemoryMarshal.Read<Size>(sizeIterator.ValueSpan),
                    LastModifiedTicks = MemoryMarshal.Read<long>(lastModifiedIterator.ValueSpan),
                },
            };
            pairs.Add(pathPartPair);
        }
        return pairs;
    }

    /// <summary>
    /// Converts Mnemonic db disk state entries to path part pairs.
    /// </summary>
    /// <param name="entries"></param>
    /// <returns></returns>
    private IEnumerable<PathPartPair> DiskStateToPathPartPair<T>(T entries) 
        where T : IEnumerable<DiskStateEntry.ReadOnly>
    {
         
        
        foreach (var entry in entries)
        {
            yield return new PathPartPair
            {
                Path = entry.Path,
                Part = new SyncNodePart
                {
                    EntityId = entry.Id,
                    Hash = entry.Hash,
                    Size = entry.Size,
                    LastModifiedTicks = entry.LastModified.UtcTicks,
                },
            };
        }
    }

    /// <inheritdoc />
    public void ProcessSyncTree(Dictionary<GamePath, SyncNode> tree)
    {
        foreach (var path in tree.Keys)
        {
            // TODO: sucks that we have to do a lookup here, but we have no way to get the ref to the value otherwise
            ref var item = ref CollectionsMarshal.GetValueRefOrNullRef(tree, path);
            
            var signature = SignatureBuilder.Build(
                diskHash: item.HaveDisk ? item.Disk.Hash : Optional<Hash>.None,
                prevHash: item.HavePrevious ? item.Previous.Hash : Optional<Hash>.None,
                loadoutHash: item.HaveLoadout && item.Loadout.Hash != Hash.Zero ? item.Loadout.Hash : Optional<Hash>.None,
                diskArchived: item.HaveDisk && HaveArchive(item.Disk.Hash),
                prevArchived: item.HavePrevious && HaveArchive(item.Previous.Hash),
                loadoutArchived: item.Loadout.Hash != Hash.Zero && HaveArchive(item.Loadout.Hash),
                pathIsIgnored: IsIgnoredBackupPath(path),
                item.SourceItemType);


            item.Signature = signature;
            item.Actions = ActionMapping.MapActions(signature);
        }
    }

    /// <inheritdoc />
    public async Task<Loadout.ReadOnly> RunActions(Dictionary<GamePath, SyncNode> syncTree, Loadout.ReadOnly loadout, SynchronizeLoadoutJob? job = null)
    {
        using var _ = await _lock.LockAsync();
        using var tx = Connection.BeginTransaction();
        var gameMetadataId = loadout.InstallationInstance.GameMetadataId;
        var register = loadout.InstallationInstance.LocationsRegister;
        HashSet<GamePath> foldersWithDeletedFiles = [];
        EntityId? overridesGroup = null;
        
        foreach (var action in ActionsInOrder)
        {
            switch (action)
            {
                case Actions.DoNothing:
                    break;

                case Actions.BackupFile:
                    job?.SetStatus("Backing up files");
                    await ActionBackupNewFiles(loadout.InstallationInstance, syncTree);
                    break;

                case Actions.IngestFromDisk:
                    job?.SetStatus("Adding external changes");
                    ActionIngestFromDisk(syncTree, loadout, tx, ref overridesGroup);
                    break;
                
                case Actions.AdaptLoadout:
                    job?.SetStatus("Updating loadout");
                    await AdaptLoadout(syncTree, register, loadout, tx);
                    break;

                case Actions.DeleteFromDisk:
                    job?.SetStatus("Deleting files");
                    ActionDeleteFromDisk(syncTree, register, tx, gameMetadataId, foldersWithDeletedFiles, job);
                    break;

                case Actions.ExtractToDisk:
                    job?.SetStatus("Extracting files");
                    await ActionExtractToDisk(syncTree, register, tx, gameMetadataId, job);
                    break;
                
                case Actions.WriteIntrinsic:
                    job?.SetStatus("Writing intrinsic files");
                    await ActionWriteIntrinsics(syncTree, register, tx, loadout, job);
                    break;

                case Actions.AddReifiedDelete:
                    job?.SetStatus("Updating deleted files");
                    ActionAddReifiedDelete(syncTree, loadout, tx, ref overridesGroup);
                    break;

                case Actions.WarnOfUnableToExtract:
                    WarnOfUnableToExtract(syncTree);
                    break;

                case Actions.WarnOfConflict:
                    WarnOfConflict(syncTree);
                    break;
                
                default:
                    throw new InvalidOperationException($"Unknown action: {action}");
            }
        }

        job?.SetStatus("Recording changes");
        
        tx.Add(gameMetadataId, GameInstallMetadata.LastSyncedLoadout, loadout.Id);
        tx.Add(gameMetadataId, GameInstallMetadata.LastSyncedLoadoutTransaction, EntityId.From(tx.ThisTxId.Value));
        tx.Add(gameMetadataId, GameInstallMetadata.LastScannedDiskStateTransaction, EntityId.From(tx.ThisTxId.Value));
        tx.Add(loadout.Id, Loadout.LastAppliedDateTime, DateTime.UtcNow);
        await tx.Commit();

        loadout = loadout.Rebase();
        var newState = loadout.Installation.DiskStateEntries;

        // Clean up empty directories
        if (foldersWithDeletedFiles.Count > 0)
        {
            CleanDirectories(foldersWithDeletedFiles, newState, loadout.InstallationInstance);
        }

        job?.SetStatus("Archive Cleanup");
        await _garbageCollectorRunner.RunAsync();


        return loadout;
    }

    private async Task ActionWriteIntrinsics(Dictionary<GamePath, SyncNode> syncTree, IGameLocationsRegister register, IMainTransaction tx, Loadout.ReadOnly loadout, SynchronizeLoadoutJob? job)
    {
        var intrinsicFiles = IntrinsicFiles(loadout);
        foreach (var (path, node) in syncTree)
        {
            if (!node.Actions.HasFlag(Actions.WriteIntrinsic))
                continue;
            
            if (node.SourceItemType != LoadoutSourceItemType.Intrinsic)
                throw new Exception("WriteIntrinsic should only be called on intrinsic files");

            var instance = intrinsicFiles[path];
            var resolvedPath = register.GetResolvedPath(path);
            resolvedPath.Parent.CreateDirectory();
            await using var stream = resolvedPath.Create();
            stream.SetLength(0);
            await instance.Write(stream, loadout, syncTree);
        }
    }

    private async Task AdaptLoadout(Dictionary<GamePath, SyncNode> syncTree, IGameLocationsRegister register, Loadout.ReadOnly loadout, IMainTransaction tx)
    {
        var intrinsicFiles = IntrinsicFiles(loadout);
        foreach (var (path, node) in syncTree)
        {
            if (!node.Actions.HasFlag(Actions.AdaptLoadout))
                continue;
            
            if (node.SourceItemType != LoadoutSourceItemType.Intrinsic)
                throw new Exception("AdaptLoadout should only be called on intrinsic files");

            var instance = intrinsicFiles[path];
            var resolvedPath = register.GetResolvedPath(path);
            await using var stream = resolvedPath.Read();
            await instance.Ingest(stream, loadout, syncTree, tx);
        }

    }

    /// <summary>
    /// Updates the locator IDs on the loadout if the game has been updated by the store.
    /// This should be called before building the sync tree.
    /// </summary>
    private async ValueTask<Loadout.ReadOnly> UpdateLocatorIds(Loadout.ReadOnly loadout)
    {
        var gameLocatorResults = loadout.InstallationInstance.Locator.Find(loadout.InstallationInstance.Game);

        // NOTE(erri120): It would be very odd if we re-query the game, and it's not installed anymore
        if (!gameLocatorResults.TryGetFirst(result => result.Store == loadout.InstallationInstance.Store, out var gameLocatorResult))
        {
            Logger.LogCritical("Found no installation of the game `{Store}`/`{Game}` anymore!", loadout.InstallationInstance.Store, loadout.InstallationInstance.Game.Name);
            return loadout;
        }

        var metadataLocatorIds = gameLocatorResult.Metadata.ToLocatorIds().ToArray();
        var newLocatorIds = metadataLocatorIds.Distinct().ToArray();

        if (newLocatorIds.Length != metadataLocatorIds.Length)
            Logger.LogWarning("Found duplicate locator IDs `{LocatorIds}` on gameLocatorResult for game `{Game}` while updating locator IDs", metadataLocatorIds, loadout.InstallationInstance.Game.Name);

        var locatorsToAdd = newLocatorIds.Except(loadout.LocatorIds).ToArray();
        var locatorsToRemove = loadout.LocatorIds.Except(newLocatorIds).ToArray();

        // No reason to change the loadout if the version is the same
        if (locatorsToAdd.Length == 0 && locatorsToRemove.Length == 0)
            return loadout;

        if (Logger.IsEnabled(LogLevel.Information))
        {
            var sCurrent = loadout.LocatorIds.Select(x => x.Value).ToArray();
            var sToAdd = locatorsToAdd.Select(x => x.Value).ToArray();
            var sToRemove = locatorsToRemove.Select(x => x.Value).ToArray();
            Logger.LogInformation("Locator IDs changed Current=`{CurrentIds}` ToAdd=`{ToAdd}` ToRemove=`{ToRemove}`", sCurrent, sToAdd, sToRemove);
        }

        using var tx = Connection.BeginTransaction();

        if (_fileHashService.TryGetVanityVersion((gameLocatorResult.Store, newLocatorIds), out var vanityVersion))
        {
            tx.Add(loadout, Loadout.GameVersion, vanityVersion);
        }
        else
        {
            tx.Add(loadout, Loadout.GameVersion, VanityVersion.DefaultValue);
            Logger.LogWarning("Found no vanity version for locator IDs `{LocatorIds}` (`{Store}`)", newLocatorIds, gameLocatorResult.Store);
        }

        foreach (var id in locatorsToRemove)
            tx.Retract(loadout, Loadout.LocatorIds, id);
        foreach (var id in locatorsToAdd)
            tx.Add(loadout, Loadout.LocatorIds, id);

        var result = await tx.Commit();
        return loadout.Rebase(result.Db);
    }

    private async ValueTask<Loadout.ReadOnly> ReprocessOverrides(Loadout.ReadOnly loadout)
    {
        // Make a lookup set of the new files based on current locator IDs
        var versionFiles = _fileHashService
            .GetGameFiles((loadout.InstallationInstance.Store, loadout.LocatorIds.ToArray()))
            .Select(file => file.Path)
            .ToHashSet();

        // Find all files in the overrides that match a path in the new files
        var toDelete = from grp in loadout.Items.OfTypeLoadoutItemGroup().OfTypeLoadoutOverridesGroup()
            from item in grp.AsLoadoutItemGroup().Children.OfTypeLoadoutItemWithTargetPath()
            let path = (GamePath)item.TargetPath
            where versionFiles.Contains(path)
            select item;

        // No files to process, return early
        if (!toDelete.Any())
            return loadout;

        using var tx = Connection.BeginTransaction();
        var gameMetadataId = loadout.InstallationInstance.GameMetadataId;

        // Delete all the matching override files
        foreach (var file in toDelete)
        {
            tx.Delete(file, recursive: false);

            // The backed up file is being 'promoted' to a game file, which needs
            // to be rooted explicitly in case the user uses a feature like 'undo'
            // to roll back a game version on a store (like Xbox/Epic) which does
            // not support downloading non-current version(s).
            if (!file.TryGetAsLoadoutFile(out var loadoutFile))
                continue;

            _ = new GameBackedUpFile.New(tx)
            {
                Hash = loadoutFile.Hash,
                GameInstallId = gameMetadataId,
            };
        }

        var result = await tx.Commit();
        return loadout.Rebase(result.Db);
    }

    /// <summary>
    /// Alternative to <see cref="RunActions"/> that ignores changes and optionally clears the last sync loadout metadata
    /// </summary>
    public async Task RunActions(Dictionary<GamePath, SyncNode> syncTree, GameInstallation gameInstallation)
    {
        using var _ = await _lock.LockAsync();
        using var tx = Connection.BeginTransaction();
        var gameMetadataId = gameInstallation.GameMetadataId;
        var gameMetadata = GameInstallMetadata.Load(Connection.Db, gameMetadataId);
        var register = gameInstallation.LocationsRegister;
        HashSet<GamePath> foldersWithDeletedFiles = [];

        foreach (var action in ActionsInOrder)
        {
            switch (action)
            {
                case Actions.DoNothing:
                    break;

                case Actions.BackupFile:
                    await ActionBackupNewFiles(gameInstallation, syncTree);
                    break;
                
                case Actions.AdaptLoadout:
                    if (ApplicationConstants.IsDebug && syncTree.Any(n => n.Value.Actions.HasFlag(Actions.AdaptLoadout)))
                        throw new InvalidOperationException("Cannot adapt loadout when not in a loadout context");
                    break;
                
                case Actions.WriteIntrinsic:
                    if (ApplicationConstants.IsDebug && syncTree.Any(n => n.Value.Actions.HasFlag(Actions.AdaptLoadout)))
                        throw new InvalidOperationException("Cannot adapt loadout when not in a loadout context");
                    break;

                case Actions.IngestFromDisk:
                    if (ApplicationConstants.IsDebug && syncTree.Any(n => n.Value.Actions.HasFlag(Actions.IngestFromDisk)))
                        throw new InvalidOperationException("Cannot ingest files from disk when not in a loadout context");
                    break;

                case Actions.DeleteFromDisk:
                    ActionDeleteFromDisk(syncTree, register, tx, gameInstallation.GameMetadataId, foldersWithDeletedFiles);
                    break;

                case Actions.ExtractToDisk:
                    await ActionExtractToDisk(syncTree, register, tx, gameMetadataId);
                    break;

                case Actions.AddReifiedDelete:
                    if (ApplicationConstants.IsDebug && syncTree.Any(n => n.Value.Actions.HasFlag(Actions.AddReifiedDelete)))
                        throw new InvalidOperationException("Cannot add reified deletes when not in a loadout context");
                    break;

                case Actions.WarnOfUnableToExtract:
                    WarnOfUnableToExtract(syncTree);
                    break;

                case Actions.WarnOfConflict:
                    WarnOfConflict(syncTree);
                    break;

                default:
                    throw new InvalidOperationException($"Unknown action: {action}");
            }
        }

        if (gameMetadata.Contains(GameInstallMetadata.LastSyncedLoadout))
        {
            tx.Retract(gameMetadataId, GameInstallMetadata.LastSyncedLoadout, (EntityId)gameMetadata.LastSyncedLoadout);
            tx.Retract(gameMetadataId, GameInstallMetadata.LastSyncedLoadoutTransaction, (EntityId)gameMetadata.LastSyncedLoadoutTransaction);
        }
        tx.Add(gameMetadataId, GameInstallMetadata.LastScannedDiskStateTransaction, EntityId.From(tx.ThisTxId.Value));

        var result = await tx.Commit();

        var newMetadata = gameMetadata.Rebase(result.Db);

        // Clean up empty directories
        if (foldersWithDeletedFiles.Count > 0)
        {
            CleanDirectories(foldersWithDeletedFiles, newMetadata.DiskStateEntries, gameInstallation);
        }
    }

    private void WarnOfConflict(Dictionary<GamePath, SyncNode> tree)
    {
        
        foreach (var (path, node) in tree)
        {
            if (!node.Actions.HasFlag(Actions.WarnOfConflict))
                continue;
            Logger.LogWarning("Conflict in {Path}", path);
        }
    }

    private void WarnOfUnableToExtract(Dictionary<GamePath, SyncNode> groupings)
    {
        foreach (var (path, node) in groupings)
        {
            if (!node.Actions.HasFlag(Actions.WarnOfUnableToExtract))
                continue;
            Logger.LogWarning("Unable to extract {Path}", path);
        }
    }

    private void ActionAddReifiedDelete(Dictionary<GamePath, SyncNode> groupings, Loadout.ReadOnly loadout, ITransaction tx, ref EntityId? overridesGroup)
    {

        foreach (var (path, node) in groupings)
        {
            if (!node.Actions.HasFlag(Actions.AddReifiedDelete))
                continue;
            
            overridesGroup ??= GetOrCreateOverridesGroup(tx, loadout);
            
            // If this is not a new entity, we may have a matching file in the overrides group already
            if (!overridesGroup.Value.InPartition(PartitionId.Temp))
            {
                var group = LoadoutOverridesGroup.Load(loadout.Db, overridesGroup.Value);
                var foundMatch = group.AsLoadoutItemGroup().Children
                    .OfTypeLoadoutItemWithTargetPath()
                    .TryGetFirst(p => p.TargetPath == path, out var match);

                if (foundMatch)
                {

                    // A delete of a delete does nothing
                    if (match.TryGetAsDeletedFile(out var _))
                        continue;

                    // If we found a match, we need to remove the entity itself
                    tx.Delete(match, recursive: false);
                    continue;
                }
            }
                
            _ = new DeletedFile.New(tx, out var id)
            {
                Reason = "Reified delete",
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(tx, id)
                {
                    TargetPath = path.ToGamePathParentTuple(loadout.Id),
                    LoadoutItem = new LoadoutItem.New(tx, id)
                    {
                        Name = path.FileName,
                        ParentId = overridesGroup.Value,
                        LoadoutId = loadout.Id,
                    },
                },
            };
        }
    }

    private async Task ActionExtractToDisk(Dictionary<GamePath, SyncNode> groupings, IGameLocationsRegister register, ITransaction tx, EntityId gameMetadataId, SynchronizeLoadoutJob? job = null)
    {
        List<(Hash Hash, AbsolutePath Path)> toExtract = [];
        
        foreach (var (path, node) in groupings)
        {
            if (!node.Actions.HasFlag(Actions.ExtractToDisk))
                continue;
            
            Debug.Assert(node.Loadout.Hash != Hash.Zero, "Loadout hash is zero, this should not happen");
            
            var gamePath = register.GetResolvedPath(path);
            toExtract.Add((node.Loadout.Hash, gamePath));
        }
        
        // Extract files to disk
        Logger.LogDebug("Extracting {Count} files to disk", toExtract.Count);
        
        if (toExtract.Count > 0)
        {
            await _fileStore.ExtractFiles(toExtract, CancellationToken.None, UpdateStatus);

            var isUnix = _os.IsUnix();
            foreach (var (gamePath, node) in groupings)
            {
                if (!node.Actions.HasFlag(Actions.ExtractToDisk))
                    continue;

                var resolvedPath = register.GetResolvedPath(gamePath);
                var writeTimeUtc = new DateTimeOffset(resolvedPath.FileInfo.LastWriteTimeUtc);
                
                // Reuse the old disk state entry if it exists
                if (node.HaveDisk)
                {
                    var id = node.Disk.EntityId;
                    tx.Add(id, DiskStateEntry.Hash, node.Loadout.Hash);
                    tx.Add(id, DiskStateEntry.Size, node.Loadout.Size);
                    tx.Add(id, DiskStateEntry.LastModified, writeTimeUtc);
                }
                else
                {
                    _ = new DiskStateEntry.New(tx, tx.TempId(DiskStateEntry.EntryPartition))
                    {
                        Path = gamePath.ToGamePathParentTuple(gameMetadataId),
                        Hash = node.Loadout.Hash,
                        Size = node.Loadout.Size,
                        LastModified = writeTimeUtc,
                        GameId = gameMetadataId,
                    };
                }


                // And mark them as executable if necessary, on Unix
                if (!isUnix)
                    continue;

                var ext = resolvedPath.Extension.ToString().ToLower();
                if (ext is not ("" or ".sh" or ".bin" or ".run" or ".py" or ".pl" or ".php" or ".rb" or ".out"
                    or ".elf")) continue;

                // Note (Sewer): I don't think we'd ever need anything other than just 'user' execute, but you can never
                // be sure. Just in case, I'll throw in group and other to match 'chmod +x' behaviour.
                var currentMode = resolvedPath.GetUnixFileMode();
                resolvedPath.SetUnixFileMode(currentMode | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
            }
        }

        void UpdateStatus((int Current, int Max) progress)
        {
            var (current, max) = progress;
            job?.SetStatus($"({current}/{max}) Extracting files");
        }
    }

    private void ActionDeleteFromDisk(
        Dictionary<GamePath, SyncNode> groupings,
        IGameLocationsRegister register,
        ITransaction tx,
        GameInstallMetadataId gameMetadataId,
        HashSet<GamePath> foldersWithDeletedFiles,
        SynchronizeLoadoutJob? job = null)
    {
        var itemIndex = 0;
        
        var deleteFileCount = groupings.Sum(static x => x.Value.Actions.HasFlag(Actions.DeleteFromDisk) ? 1 : 0);
        
        // Delete files from disk
        foreach (var (path, node) in groupings)
        {
            if (!node.Actions.HasFlag(Actions.DeleteFromDisk))
                continue;

            if (itemIndex % 1000f == 0)
                job?.SetStatus($"({itemIndex}/{deleteFileCount}) Deleting files");
            itemIndex++;
            
            var resolvedPath = register.GetResolvedPath(path);
            resolvedPath.Delete();

            
            // Only delete the entry if we're not going to replace it
            if (!node.Actions.HasFlag(Actions.ExtractToDisk))
            {
                foldersWithDeletedFiles.Add(path.Parent);

                var id = node.Disk.EntityId;
                tx.Retract(id, DiskStateEntry.Path, ((EntityId)gameMetadataId, path.LocationId, path.Path));
                tx.Retract(id, DiskStateEntry.Hash, node.Disk.Hash);
                tx.Retract(id, DiskStateEntry.Size, node.Disk.Size);
                tx.Retract(id, DiskStateEntry.LastModified, new DateTimeOffset(node.Disk.LastModifiedTicks, TimeSpan.Zero));
                tx.Retract(id, DiskStateEntry.Game, (EntityId)gameMetadataId);
            }
        }
    }

    public record struct AddedEntry
    {
        public required LoadoutItem.New LoadoutItem { get; init; }
        public required LoadoutItemWithTargetPath.New LoadoutItemWithTargetPath { get; init; }
        public required LoadoutFile.New LoadoutFileEntry { get; init; }
    }

    private bool ActionIngestFromDisk(Dictionary<GamePath, SyncNode> syncTree, Loadout.ReadOnly loadout, ITransaction tx, ref EntityId? overridesGroupId)
    {
        overridesGroupId ??= GetOrCreateOverridesGroup(tx, loadout);
        var newGroup = true;
        LoadoutItemGroup.ReadOnly? overridesGroup = null;
        if (!overridesGroupId.Value.InPartition(PartitionId.Temp))
        {
            newGroup = false;
            overridesGroup = LoadoutItemGroup.Load(loadout.Db, overridesGroupId.Value);
        }

        var ingestedFiles = false;
        
        foreach (var (path, node) in syncTree)
        {
            if (!node.Actions.HasFlag(Actions.IngestFromDisk))
                continue;

            // If the overrides group is not new, we need to check if the file is already in the overrides group
            if (!newGroup)
            {
                var existingRecord = overridesGroup!.Value.Children
                    .OfTypeLoadoutItemWithTargetPath()
                    .FirstOrOptional(c => c.TargetPath == path);

                if (existingRecord.HasValue)
                {
                    // Update the disk entry
                    tx.Add(node.Disk.EntityId, DiskStateEntry.LastModified, new DateTimeOffset(node.Disk.LastModifiedTicks, TimeSpan.Zero));
                    
                    // Update the file entry
                    tx.Add(existingRecord.Value.Id, LoadoutFile.Hash, node.Disk.Hash);
                    tx.Add(existingRecord.Value.Id, LoadoutFile.Size, node.Disk.Size);
                    
                    // Mark that we ingested a file
                    ingestedFiles = true;
                    
                    // Skip the rest of this process
                    continue;
                }
            }

            // Entry was added
            var id = tx.TempId();
            var loadoutItem = new LoadoutItem.New(tx, id)
            {
                ParentId = overridesGroupId.Value,
                LoadoutId = loadout.Id,
                Name = path.FileName,
            };
            var loadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(tx, id)
            {
                LoadoutItem = loadoutItem,
                TargetPath = path.ToGamePathParentTuple(loadout.Id),
            };

            _ = new LoadoutFile.New(tx, id)
            {
                LoadoutItemWithTargetPath = loadoutItemWithTargetPath,
                Hash = node.Disk.Hash,
                Size = node.Disk.Size,
            };
            tx.Add(node.Disk.EntityId, DiskStateEntry.LastModified, new DateTimeOffset(node.Disk.LastModifiedTicks, TimeSpan.Zero));
            ingestedFiles = true;
        }

        return ingestedFiles;
    }

    /// <inheritdoc />
    public virtual async Task<Loadout.ReadOnly> Synchronize(Loadout.ReadOnly loadout, SynchronizeLoadoutJob? job = null)
    {
        loadout = loadout.Rebase();
        // If we are swapping loadouts, then we need to synchronize the previous loadout first to ingest
        // any changes, then we can apply the new loadout.
        if (GameInstallMetadata.LastSyncedLoadout.TryGetValue(loadout.Installation, out var lastAppliedId) && lastAppliedId != loadout.Id)
        {
            var prevLoadout = Loadout.Load(loadout.Db, lastAppliedId);
            if (prevLoadout.IsValid())
            {
                await DeactivateCurrentLoadout(loadout.InstallationInstance);
                await ActivateLoadout(loadout);
                return loadout.Rebase();
            }
        }

        // Update locator IDs before building the sync tree
        loadout = await UpdateLocatorIds(loadout);

        job?.SetStatus("Collecting files");
        var tree = await BuildSyncTree(loadout);
        ProcessSyncTree(tree);
        loadout = await RunActions(tree, loadout, job);

        // Move any override files that now match game files after sync
        loadout = await ReprocessOverrides(loadout);
        return loadout;
    }

    public async Task<GameInstallMetadata.ReadOnly> RescanFiles(GameInstallation gameInstallation, bool ignoreModifiedDates)
    {
        // Make sure the file hashes are up to date
        await _fileHashService.GetFileHashesDb();
        return await ReindexState(gameInstallation, ignoreModifiedDates, Connection);
    }

    /// <summary>
    /// All actions, in execution order.
    /// </summary>
    private static readonly Actions[] ActionsInOrder = Enum.GetValues<Actions>().OrderBy(a => (ushort)a).ToArray();

    /// <summary>
    /// Returns true if the given hash has been archived.
    /// </summary>
    protected bool HaveArchive(Hash hash)
    {
        return _fileStore.HaveFile(hash).Result;
    }
    
    /// <summary>
    /// Return true if the replacement should win over the existing item.
    /// </summary>
    private bool ShouldWinWrapper(IDb db, in GamePath path, in SyncNodePart existing, LoadoutSourceItemType existingItemType, in SyncNodePart replacement, LoadoutSourceItemType replacementItemType)
    {
        // Game files always lose
        if (existingItemType == LoadoutSourceItemType.Game)
            return true;
        
        // TODO: we don't often have many files that conflict by name, but we should speed this code up at some point
        var existingLoadoutItem = LoadoutItem.Load(db, existing.EntityId);
        var existingInOverrides = existingLoadoutItem
            .GetThisAndParents()
            .OfTypeLoadoutItemGroup()
            .OfTypeLoadoutOverridesGroup()
            .Any();
        
        var replacementLoadoutItem = LoadoutItem.Load(db, replacement.EntityId);
        var replacementInOverrides = replacementLoadoutItem
            .GetThisAndParents()
            .OfTypeLoadoutItemGroup()
            .OfTypeLoadoutOverridesGroup()
            .Any();
        
        // Overrides always win
        if (existingInOverrides && !replacementInOverrides)
            return false;
        else if (!existingInOverrides && replacementInOverrides)
            return true;
        else if (existingInOverrides && replacementInOverrides)
            return false;
        
        // Return the newer one
        return ShouldWin(path, existingLoadoutItem, replacementLoadoutItem);
    }

    /// <summary>
    /// Returns true if <paramref name="b"/> should win over <paramref name="a"/> based
    /// on rules defined in a collection.
    /// </summary>
    protected Optional<bool> ShouldOtherWinWithCollectionRules(LoadoutItem.ReadOnly a, LoadoutItem.ReadOnly b)
    {
        var hasDownloadA = a
            .GetThisAndParents()
            .OfTypeLoadoutItemGroup()
            .OfTypeNexusCollectionItemLoadoutGroup()
            .Select(static item => item.Download)
            .TryGetFirst(out var downloadA);

        if (!hasDownloadA) return Optional<bool>.None;
        var hasDownloadB = b
            .GetThisAndParents()
            .OfTypeLoadoutItemGroup()
            .OfTypeNexusCollectionItemLoadoutGroup()
            .Select(static item => item.Download)
            .TryGetFirst(out var downloadB);

        if (!hasDownloadB) return Optional<bool>.None;
        if (downloadA.CollectionRevisionId != downloadB.CollectionRevisionId) return Optional<bool>.None;

        var db = downloadA.Db;

        // use `downloadA` as `source` and `downloadB` as `other`
        var ids = db.Datoms(
            (CollectionDownloadRules.Source, downloadA),
            (CollectionDownloadRules.Other, downloadB)
        );

        foreach (var id in ids)
        {
            var rule = CollectionDownloadRules.Load(db, id);
            if (!rule.IsValid()) continue;

            // `source` goes before `other`, meaning `a` doesn't win over `b`
            if (rule.RuleType == CollectionDownloadRuleType.Before) return true;

            // `source` goes after `other`, meaning `a` wins over `b`
            if (rule.RuleType == CollectionDownloadRuleType.After) return false;
        }

        // use `downloadB` as `source` and `downloadA` as `other`
        ids = db.Datoms(
            (CollectionDownloadRules.Source, downloadB),
            (CollectionDownloadRules.Other, downloadA)
        );

        foreach (var id in ids)
        {
            var rule = CollectionDownloadRules.Load(db, id);
            if (!rule.IsValid()) continue;

            // `source` goes after `other`, meaning `b` wins over `a`
            if (rule.RuleType == CollectionDownloadRuleType.After) return true;

            // `source` goes before `other`, meaning `b` doesn't win over `a`
            if (rule.RuleType == CollectionDownloadRuleType.Before) return false;
        }

        return Optional<bool>.None;
    }

    /// <summary>
    /// Override this method to provide custom logic for determining which file should win in a conflict. Return true if the
    /// replacement should win over the existing item.
    /// </summary>
    protected virtual bool ShouldWin(in GamePath path, LoadoutItem.ReadOnly existing, LoadoutItem.ReadOnly replacement)
    {
        var optional = ShouldOtherWinWithCollectionRules(existing, replacement);
        if (optional.HasValue) return optional.Value;

        return replacement.Id > existing.Id;
    }
    
    /// <summary>
    /// Returns true if the loadout state doesn't match the last scanned disk state.
    /// </summary>
    public bool ShouldSynchronize(Loadout.ReadOnly loadout, IEnumerable<PathPartPair> previousDiskState, IEnumerable<PathPartPair> lastScannedDiskState)
    {
        var syncTree = BuildSyncTree(lastScannedDiskState, previousDiskState, loadout);
        // Process the sync tree to get the actions populated in the nodes
        ProcessSyncTree(syncTree);
        
        return syncTree.Any(n => n.Value.Actions != Actions.DoNothing);
    }
    
    /// <inheritdoc />
    public FileDiffTree LoadoutToDiskDiff(Loadout.ReadOnly loadout, List<PathPartPair> previousDiskState, List<PathPartPair> lastScannedDiskState)
    {
        var syncTree = BuildSyncTree(lastScannedDiskState, previousDiskState, loadout);
        // Process the sync tree to get the actions populated in the nodes
        ProcessSyncTree(syncTree);

        List<KeyValuePair<GamePath, DiskDiffEntry>> diffs = [];

        foreach (var (path, node) in syncTree)
        {
            var syncNode = node;
            var actions = syncNode.Actions;
            DiskDiffEntry entry;
            
            if (actions.HasFlag(Actions.DoNothing))
            {
                entry = new DiskDiffEntry
                {
                    Hash = node.Loadout.Hash,
                    Size = node.Loadout.Size,
                    ChangeType = FileChangeType.None,
                    GamePath = path,
                };
                diffs.Add(KeyValuePair.Create(path, entry));
            }
            else if (actions.HasFlag(Actions.WarnOfUnableToExtract))
            {
                entry = new DiskDiffEntry
                {
                    Hash = node.Loadout.Hash,
                    Size = node.Loadout.Size,
                    ChangeType = FileChangeType.Added,
                    GamePath = path,
                };
            }
            else if (actions.HasFlag(Actions.ExtractToDisk))
            {
                entry = new DiskDiffEntry
                {
                    Hash = node.Loadout.Hash,
                    Size = node.Loadout.Size,
                    // If paired with a delete action, this is a modified file not a new one
                    ChangeType = actions.HasFlag(Actions.DeleteFromDisk) ? FileChangeType.Modified : FileChangeType.Added,
                    GamePath = path,
                };
            }
            else if (actions.HasFlag(Actions.DeleteFromDisk))
            {
                entry = new DiskDiffEntry
                {
                    Hash = node.Disk.Hash,
                    Size = node.Disk.Size,
                    ChangeType = FileChangeType.Removed,
                    GamePath = path,
                };
            }
            else if (actions.HasFlag(Actions.IngestFromDisk))
            {
                // File is already on disk and will not be changed
                entry = new DiskDiffEntry
                {
                    Hash = node.Disk.Hash,
                    Size = node.Disk.Size,
                    ChangeType = FileChangeType.None,
                    GamePath = path,
                };
            }
            else if (actions.HasFlag(Actions.AddReifiedDelete))
            {
                // File is not on disk and will not end up on disk, so don't show it
                continue;
            }
            else
            {
                // This really should become some sort of error state
                entry = new DiskDiffEntry
                {
                    Hash = Hash.Zero,
                    Size = Size.Zero,
                    ChangeType = FileChangeType.None,
                    GamePath = path,
                };
            }
            
            diffs.Add(KeyValuePair.Create(path, entry));
        }

        return FileDiffTree.Create(diffs);
    }

    /// <summary>
    /// Backs up any new files in the loadout.
    ///
    /// </summary>
    public virtual async Task ActionBackupNewFiles(GameInstallation installation, Dictionary<GamePath, SyncNode> files)
    {
        // During ingest, new files that haven't been seen before are fed into the game's synchronizer to convert a
        // DiskStateEntry (hash, size, path) into some sort of LoadoutItem. By default, these are converted into a "LoadoutFile".
        // All Loadoutfile does, is say that this file is copied from the downloaded archives, that is, it's not generated
        // by any extension system.
        //
        // So the problem is, the ingest process has tagged all these new files as coming from the downloads, but likely
        // they've never actually been copied/compressed into the download folders. So if we need to restore them they won't exist.
        //
        // If a game wants other types of files to be backed up, they could do so with their own logic. But backing up a
        // IGeneratedFile is pointless, since when it comes time to restore that file we'll call file.Generate on it since
        // it's a generated file.

        // TODO: This may be slow for very large games when other games/mods already exist.
        // Backup the files that are new or changed
        var archivedFiles = new ConcurrentBag<ArchivedFileEntry>();
        var pinnedFileHashes = new ConcurrentBag<Hash>();
        await Parallel.ForEachAsync(files, async (item, _) =>
            {
                var (gamePath, node) = item;
                if (!node.Actions.HasFlag(Actions.BackupFile))
                    return;
                
                var path = installation.LocationsRegister.GetResolvedPath(gamePath);
                Debug.Assert(node.HaveDisk, "Node must have a disk entry to backup");
                
                if (await _fileStore.HaveFile(node.Disk.Hash))
                    return;
                
                var archivedFile = new ArchivedFileEntry
                {
                    Size = node.Disk.Size,
                    Hash = node.Disk.Hash,
                    StreamFactory = new NativeFileStreamFactory(path),
                };

                archivedFiles.Add(archivedFile);
                if (node.SourceItemType == LoadoutSourceItemType.Game)
                    pinnedFileHashes.Add(archivedFile.Hash);
            }
        );

        var totalSize = archivedFiles.Sum(static x => x.Size);
        if (totalSize > MaximumBackupSize)
            throw new Exception($"Cannot backup files, total size is {totalSize}, which is larger than the maximum of {MaximumBackupSize}");
        
        // PERFORMANCE: We deduplicate above with the HaveFile call.
        await _fileStore.BackupFiles(archivedFiles, deduplicate: false);

        // Pin the files to avoid garbage collection.
        using var tx = Connection.BeginTransaction();
        foreach (var hash in pinnedFileHashes)
        {
            _ = new GameBackedUpFile.New(tx)
            {
                GameInstallId = installation.GameMetadataId,
                Hash = hash,
            };
        }
        await tx.Commit();
    }
    
    /// <summary>
    /// Reindex the state of the game, running a transaction if changes are found
    /// </summary>
    private async Task<GameInstallMetadata.ReadOnly> ReindexState(GameInstallation installation, bool ignoreModifiedDates, IConnection connection)
    {        
        using var _ = await _lock.LockAsync();
        var originalMetadata = installation.GetMetadata(connection);
        using var tx = connection.BeginTransaction();

        // Index the state
        var changed = await ReindexState(installation, ignoreModifiedDates, connection, tx);
        
        if (!originalMetadata.Contains(GameInstallMetadata.InitialDiskStateTransaction))
        {
            // No initial state, so set this transaction as the initial state
            changed = true;
            tx.Add(originalMetadata.Id, GameInstallMetadata.InitialDiskStateTransaction, EntityId.From(TxId.Tmp.Value));
        }
        
        if (changed)
        {
            tx.Add(installation.GameMetadataId, GameInstallMetadata.LastScannedDiskStateTransactionId, EntityId.From(TxId.Tmp.Value));
            await tx.Commit();
        }
        
        return GameInstallMetadata.Load(connection.Db, installation.GameMetadataId);
    }
    
    /// <summary>
    /// Reindex the state of the game
    /// </summary>
    public async Task<bool> ReindexState(GameInstallation installation, bool ignoreModifiedDates, IConnection connection, ITransaction tx)
    {
        var hashDb = await _fileHashService.GetFileHashesDb();

        var gameInstallMetadata = GameInstallMetadata.Load(connection.Db, installation.GameMetadataId);

        var previousDiskStateEntities = gameInstallMetadata.DiskStateEntries;
        var previousDiskState = new Dictionary<GamePath, DiskStateEntry.ReadOnly>(capacity: previousDiskStateEntities.Count);

        foreach (var previousDiskStateEntity in previousDiskStateEntities)
        {
            GamePath path = previousDiskStateEntity.Path;

            ref var diskStateEntity = ref CollectionsMarshal.GetValueRefOrAddDefault(previousDiskState, path, out var hasExistingDiskStateEntity);
            if (hasExistingDiskStateEntity)
            {
                Logger.LogWarning("Duplicate path in disk state: `{Path}`", path);
            }

            diskStateEntity = previousDiskStateEntity;
        }

        var hasDiskStateChanged = false;

        var seenPaths = new HashSet<GamePath>();
        var seenPathsLock = new Lock();

        foreach (var locationPair in installation.LocationsRegister.GetTopLevelLocations())
        {
            var (_, locationPath) = locationPair;
            if (!locationPath.DirectoryExists()) continue;

            await Parallel.ForEachAsync(locationPath.EnumerateFiles(), async (file, token) =>
            {
                try
                {
                    var gamePath = installation.LocationsRegister.ToGamePath(file);
                    if (ShouldIgnorePathWhenIndexing(gamePath)) return;

                    bool isNewPath;
                    lock (seenPathsLock)
                    {
                        isNewPath = seenPaths.Add(gamePath);
                    }

                    if (!isNewPath)
                    {
                        Logger.LogDebug("Skipping already indexed file at `{Path}`", file);
                        return;
                    }

                    if (previousDiskState.TryGetValue(gamePath, out var previousDiskStateEntry))
                    {
                        var fileInfo = file.FileInfo;
                        var writeTimeUtc = new DateTimeOffset(fileInfo.LastWriteTimeUtc);

                        // If the files don't match, update the entry
                        if (writeTimeUtc != previousDiskStateEntry.LastModified || fileInfo.Size != previousDiskStateEntry.Size || ignoreModifiedDates)
                        {
                            var newHash = await MaybeHashFile(hashDb, gamePath, file,
                                fileInfo, token
                            );
                            tx.Add(previousDiskStateEntry.Id, DiskStateEntry.Size, fileInfo.Size);
                            tx.Add(previousDiskStateEntry.Id, DiskStateEntry.Hash, newHash);
                            tx.Add(previousDiskStateEntry.Id, DiskStateEntry.LastModified, writeTimeUtc);
                            hasDiskStateChanged = true;
                        }
                    }
                    else
                    {
                        var newHash = await MaybeHashFile(hashDb, gamePath, file,
                            file.FileInfo, token
                        );

                        _ = new DiskStateEntry.New(tx, tx.TempId(DiskStateEntry.EntryPartition))
                        {
                            Path = gamePath.ToGamePathParentTuple(gameInstallMetadata.Id),
                            Hash = newHash,
                            Size = file.FileInfo.Size,
                            LastModified = file.FileInfo.LastWriteTimeUtc,
                            GameId = gameInstallMetadata.Id,
                        };

                        hasDiskStateChanged = true;
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
        }

        // NOTE(erri120): remove files from the disk state that don't exist on disk anymore
        foreach (var entry in previousDiskState.Values)
        {
            if (seenPaths.Contains(entry.Path)) continue;
            tx.Delete(entry.Id, recursive: false);
            hasDiskStateChanged = true;
        }

        if (hasDiskStateChanged) tx.Add(gameInstallMetadata.Id, GameInstallMetadata.LastScannedDiskStateTransaction, EntityId.From(TxId.Tmp.Value));
        return hasDiskStateChanged;
    }

    private async ValueTask<Hash> MaybeHashFile(IDb hashDb, GamePath gamePath, AbsolutePath file, IFileEntry fileInfo, CancellationToken token)
    {
        Hash? diskMinimalHash = null;

        var foundHash = Hash.Zero;
        var needFullHash = true;
        
        // Look for all known files that match the path
        foreach (var matchingPath in PathHashRelation.FindByPath(hashDb, gamePath.Path))
        {
            // Make sure the size matches
            var hash = matchingPath.Hash;
            if (hash.Size.Value != fileInfo.Size)
                continue;
            
            // If the minimal hash matches, then we can use the xxHash3 hash
            await using (var fileStream = file.Read())
            {
                diskMinimalHash ??= await MultiHasher.MinimalHash(fileStream, cancellationToken: token);
            }

            if (hash.MinimalHash == diskMinimalHash)
            {
                // We previously found a hash that matches the minimal hash, make sure the xxHash3 matches, otherwise we 
                // have a hash collision
                if (foundHash != Hash.Zero && foundHash != hash.XxHash3)
                {
                    // We have a hash collision, so we need to do a full hash
                    needFullHash = true;
                    break;
                }

                // Store the hash
                foundHash = hash.XxHash3;
                needFullHash = false;
            }
        }
        
        if (!needFullHash)
            return foundHash;

        Logger.LogDebug("Didn't find matching hash data for file `{Path}` or found multiple matches, falling back to doing a full hash", file);
        return await file.XxHash3Async(token: token);
    }

    /// <inheritdoc />
    public virtual IJobTask<CreateLoadoutJob, Loadout.ReadOnly> CreateLoadout(GameInstallation installation, string? suggestedName = null)
    {

        return _jobMonitor.Begin(new CreateLoadoutJob(installation), async ctx =>
            {
                // Prime the hash database to make sure it's loaded
                await _fileHashService.GetFileHashesDb();

                var shortName = GetNewShortName(Connection.Db, installation.GameMetadataId);

                using var tx = Connection.BeginTransaction();

                List<LocatorId> locatorIds = [];
                if (installation.LocatorResultMetadata != null)
                {
                    var metadataLocatorIds = installation.LocatorResultMetadata.ToLocatorIds().ToArray();
                    var distinctLocatorIds = metadataLocatorIds.Distinct().ToArray();
                    
                    if (distinctLocatorIds.Length != metadataLocatorIds.Length)
                    {
                        Logger.LogWarning("Duplicate locator ids `{LocatorIds}` found in LocatorResultMetadata for {Game} when creating new loadout", metadataLocatorIds, installation.Game.Name);
                    }
                    locatorIds.AddRange(distinctLocatorIds);
                }

                if (!_fileHashService.TryGetVanityVersion((installation.Store, locatorIds.ToArray()), out var version))
                    Logger.LogWarning("Unable to find game version for {Game}", installation.GameMetadataId);

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

                // Commit the transaction as of this point the loadout is live
                var result = await tx.Commit();

                // Remap the id
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
            }
        );
    }
    

    /// <inheritdoc />
    public async Task DeactivateCurrentLoadout(GameInstallation installation)
    {
        var metadata = installation.GetMetadata(Connection);
        
        if (!metadata.Contains(GameInstallMetadata.LastSyncedLoadout))
            return;
        
        // Synchronize the last applied loadout, so we don't lose any changes
        await Synchronize(Loadout.Load(Connection.Db, metadata.LastSyncedLoadout));
        
        var metadataLocatorIds = installation.LocatorResultMetadata?.ToLocatorIds().ToArray() ?? [];
        var locatorIds = metadataLocatorIds.Distinct().ToArray();
        
        if (locatorIds.Length != metadataLocatorIds.Length)
        {
            Logger.LogWarning("Duplicate locator ids `{LocatorIds}` found in LocatorResultMetadata for {Game} when deactivating loadout", metadataLocatorIds, installation.Game.Name);
        }
        
        await ResetToOriginalGameState(installation, locatorIds);
    }

    /// <inheritdoc />
    public Optional<LoadoutId> GetCurrentlyActiveLoadout(GameInstallation installation)
    {
        var metadata = installation.GetMetadata(Connection);
        if (!GameInstallMetadata.LastSyncedLoadout.TryGetValue(metadata, out var lastAppliedLoadout))
            return Optional<LoadoutId>.None;
        return LoadoutId.From(lastAppliedLoadout);
    }

    public async Task ActivateLoadout(LoadoutId loadoutId)
    {
        var loadout = Loadout.Load(Connection.Db, loadoutId);
        var reindexed = await ReindexState(loadout.InstallationInstance, ignoreModifiedDates: false, Connection);
        
        var tree = BuildSyncTree(DiskStateToPathPartPair(reindexed.DiskStateEntries), DiskStateToPathPartPair(reindexed.DiskStateEntries), loadout);
        ProcessSyncTree(tree);
        await RunActions(tree, loadout);
    }

    private LoadoutGameFilesGroup.New CreateLoadoutGameFilesGroup(LoadoutId loadout, GameInstallation installation, ITransaction tx)
    {
        return new LoadoutGameFilesGroup.New(tx, out var id)
        {
            GameMetadataId = installation.GameMetadataId,
            LoadoutItemGroup = new LoadoutItemGroup.New(tx, id)
            {
                IsGroup = true,
                LoadoutItem = new LoadoutItem.New(tx, id)
                {
                    Name = "Game Files",
                    LoadoutId = loadout,
                },
            },
        };
    }

    /// <inheritdoc />
    public async Task UnManage(GameInstallation installation, bool runGc = true, bool cleanGameFolder = true)
    {
        await _jobMonitor.Begin(new UnmanageGameJob(installation), async ctx =>
            {
                var metadata = installation.GetMetadata(Connection);

                if (GetCurrentlyActiveLoadout(installation).HasValue && cleanGameFolder)
                    await DeactivateCurrentLoadout(installation);

                await ctx.YieldAsync();

                {
                    using var tx1 = Connection.BeginTransaction();
                    foreach (var loadout in metadata.Loadouts)
                    {
                        tx1.Add(loadout.Id, Loadout.LoadoutKind, LoadoutKind.Deleted);
                    }
                    await tx1.Commit();

                    metadata = installation.GetMetadata(Connection);
                    Debug.Assert(metadata.Loadouts.All(x => !x.IsVisible()), "all loadouts shouldn't be visible anymore");
                }

                foreach (var loadout in metadata.Loadouts)
                {
                    Logger.LogInformation("Deleting loadout {Loadout} - {ShortName}", loadout.Name, loadout.ShortName);
                    await ctx.YieldAsync();
                    await DeleteLoadout(loadout, GarbageCollectorRunMode.DoNotRun, deactivateIfActive: cleanGameFolder);
                }
                
                // Retract all `GameBakedUpFile` entries to allow for game file backups to be cleaned up from the FileStore
                using var tx = Connection.BeginTransaction();
                foreach (var file in GameBackedUpFile.All(Connection.Db))
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
                    tx.Retract(metadata, GameInstallMetadata.LastScannedDiskStateTransactionId ,metadata.LastScannedDiskStateTransactionId.Value);
                
                await tx.Commit();

                if (runGc)
                    _garbageCollectorRunner.Run();
                return installation;
            }
        );
    }

    /// <inheritdoc />
    public virtual bool IsIgnoredBackupPath(GamePath path) => false;

    /// <summary>
    /// Whether to ignore the file at the given path when indexing.
    /// </summary>
    /// <remarks>
    /// Files ignored by this method will not be included in the sync tree. Prefer not including
    /// the path in the first place instead of using this method.
    /// </remarks>
    protected virtual bool ShouldIgnorePathWhenIndexing(GamePath path) => false;

    /// <inheritdoc />
    public async Task<Loadout.ReadOnly> CopyLoadout(LoadoutId loadoutId)
    {
        var baseDb = Connection.Db;
        var loadout = Loadout.Load(baseDb, loadoutId);

        // Temp space for datom values
        Memory<byte> buffer = System.GC.AllocateUninitializedArray<byte>(32);
        
        // Cache some attribute ids
        var cache = baseDb.AttributeCache;
        var nameId = cache.GetAttributeId(Loadout.Name.Id);
        var shortNameId = cache.GetAttributeId(Loadout.ShortName.Id);

        // Create a mapping of old entity ids to new (temp) entity ids
        Dictionary<EntityId, EntityId> entityIdList = new();
        var remapFn = RemapFn;
        
        using var tx = Connection.BeginTransaction();

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

                // Make sure we have enough buffer space
                if (buffer.Length < datom.ValueSpan.Length)
                    buffer = System.GC.AllocateUninitializedArray<byte>(datom.ValueSpan.Length);
                
                // Copy the value over
                datom.ValueSpan.CopyTo(buffer.Span);
                
                // Create the new datom and reference the copied value
                var prefix = new KeyPrefix(newId, datom.A, TxId.Tmp, isRetract: false, datom.Prefix.ValueTag);
                var newDatom = new Datom(prefix, buffer[..datom.ValueSpan.Length]);
                
                // Remap any entity ids in the value
                datom.Prefix.ValueTag.Remap(buffer[..datom.ValueSpan.Length].Span, remapFn);
                
                // Add the new datom
                tx.Add(newDatom);
            }
        }

        // NOTE(erri120): using latest DB to prevent duplicate short names
        var newShortName = GetNewShortName(Connection.Db, loadout.InstallationId);
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

    /// <summary>
    /// Gets a set of files intrinsic to this game. Such as mod order files, preference files, etc.
    /// These files will not be backed up and will not be included in the loadout directly. Instead, they are
    /// generated at sync time by calling the implementations of the files themselves. 
    /// </summary>
    protected virtual Dictionary<GamePath, IIntrinsicFile> IntrinsicFiles(Loadout.ReadOnly loadout)
    {
        return new();
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

    /// <inheritdoc />
    public async Task DeleteLoadout(LoadoutId loadoutId, GarbageCollectorRunMode gcRunMode = GarbageCollectorRunMode.RunAsynchronously, bool deactivateIfActive = true)
    {
        {
            using var tx1 = Connection.BeginTransaction();
            tx1.Add(loadoutId, Loadout.LoadoutKind, LoadoutKind.Deleted);
            await tx1.Commit();
        }

        var loadout = Loadout.Load(Connection.Db, loadoutId);
        Debug.Assert(!loadout.IsVisible(), "loadout shouldn't be visible anymore");

        var metadata = GameInstallMetadata.Load(Connection.Db, loadout.InstallationInstance.GameMetadataId);
        if (deactivateIfActive && GameInstallMetadata.LastSyncedLoadout.TryGetValue(metadata, out var lastAppliedLoadout) && lastAppliedLoadout == loadoutId.Value)
        {
            await DeactivateCurrentLoadout(loadout.InstallationInstance);
        }
        
        using var tx = Connection.BeginTransaction();
        tx.Delete(loadoutId, recursive: false);
        foreach (var item in loadout.Items)
        {
            tx.Delete(item.Id, recursive: false);
        }
        await tx.Commit();
        
        // Execute the garbage collector
        await _garbageCollectorRunner.RunWithMode(gcRunMode);
    }

    public async Task ResetToOriginalGameState(GameInstallation installation, LocatorId[] locatorIds)
    {
        var gameState = _fileHashService.GetGameFiles((installation.Store, locatorIds));
        var metaData = await ReindexState(installation, ignoreModifiedDates: false, Connection);

        List<PathPartPair> diskState = [];

        foreach (var diskFile in metaData.DiskStateEntries)
        {
            diskState.Add(new PathPartPair(diskFile.Path, new SyncNodePart
            {   
                EntityId = diskFile.Id,
                Hash = diskFile.Hash,
                Size = diskFile.Size,
                LastModifiedTicks = diskFile.LastModified.UtcTicks,
            }));
        }

        Dictionary<GamePath, SyncNode> desiredState = new();

        foreach (var gameFile in gameState)
        {
            var part = new SyncNodePart
            {
                Hash = gameFile.Hash,
                Size = gameFile.Size,
                LastModifiedTicks = 0,
            };
            var syncNode = new SyncNode
            {
                Loadout = part,
                SourceItemType = LoadoutSourceItemType.Game,
            };
            desiredState.Add(gameFile.Path, syncNode);
        }

        // Merge the states into a tree. Passing in the current state as the current and previous state. 
        // This fakes the synchronizer into thinking that there are no changes on disk and we only want to do a
        // hard reset to the desired state.
        MergeStates(diskState, diskState, desiredState);
        
        // Process the tree
        ProcessSyncTree(desiredState);

        // Run the groupings
        await RunActions(desiredState, installation);
    }
}

#endregion
