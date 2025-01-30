using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Trees;
using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.Games.FileHashes.Models;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Hashes;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.Loadouts.Files.Diff;
using NexusMods.Abstractions.Loadouts.Sorting;
using NexusMods.Abstractions.Loadouts.Synchronizers.Rules;
using NexusMods.Extensions.BCL;
using NexusMods.Hashing.xxHash3;
using NexusMods.Hashing.xxHash3.Paths;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

using DiskState = Entities<DiskStateEntry.ReadOnly>;

/// <summary>
/// Base class for loadout synchronizers, provides some common functionality. Does not have to be user,
/// but reduces a lot of boilerplate, and is highly recommended.
/// </summary>
[PublicAPI]
public class ALoadoutSynchronizer : ILoadoutSynchronizer
{
    private readonly IFileStore _fileStore;

    private readonly ILogger _logger;
    private readonly IOSInformation _os;
    private readonly ISorter _sorter;
    private readonly IGarbageCollectorRunner _garbageCollectorRunner;
    private readonly IServiceProvider _serviceProvider;

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
        _jobMonitor = serviceProvider.GetRequiredService<IJobMonitor>();
        _fileHashService = fileHashService;

        _logger = logger;
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

    private void CleanDirectories(IEnumerable<GamePath> toDelete, DiskState newState, GameInstallation installation)
    {
        var seenDirectories = new HashSet<GamePath>();
        var directoriesToDelete = new HashSet<GamePath>();

        var newStatePaths = newState.SelectMany(e =>
            {
                // We need folder paths, so skip first path as it is the file path itself
                return ((GamePath)e.Path).GetAllParents().Skip(1);
            }
        ).ToHashSet();

        foreach (var entry in toDelete)
        {
            var parentPath = entry.Parent;
            GamePath? emptyStructureRoot = null;
            while (parentPath != entry.GetRootComponent)
            {
                if (seenDirectories.Contains(parentPath))
                {
                    emptyStructureRoot = null;
                    break;
                }

                // newTree was build from files, so if the parent is in the new tree, it's not empty
                if (newStatePaths.Contains(parentPath))
                {
                    break;
                }

                seenDirectories.Add(parentPath);
                emptyStructureRoot = parentPath;
                parentPath = parentPath.Parent;
            }

            if (emptyStructureRoot != null)
                directoriesToDelete.Add(emptyStructureRoot.Value);
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



    public Dictionary<GamePath, SyncTreeNode> BuildSyncTree(IEnumerable<PathPartPair> currentState, IEnumerable<PathPartPair> previousState, Loadout.ReadOnly loadout)
    {
        var referenceDb = _fileHashService.Current;
        Dictionary<GamePath, SyncTreeNode> results = new();
        
        foreach (var gameFile in GetNormalGameState(referenceDb, loadout))
        {
            results.Add(gameFile.Path, new SyncTreeNode
                {
                    Loadout = new SyncNodePart
                    {
                        Hash = gameFile.Hash,
                        Size = gameFile.Size,
                    },
                    SourceItemType = LoadoutSourceItemType.Game,
                }
            );
        }
        
        var disabledGroups = GetDisabledGroups(loadout);

        foreach (var loadoutItem in loadout.Items.OfTypeLoadoutItemWithTargetPath())
        {
            var targetPath = loadoutItem.TargetPath;
            // Ignore disabled Items
            if (disabledGroups.Contains(loadoutItem.AsLoadoutItem().Parent))
                continue;

            SyncNodePart sourceItem;
            LoadoutSourceItemType sourceItemType;
            if (loadoutItem.TryGetAsLoadoutFile(out var loadutFile))
            {
                sourceItem = new SyncNodePart()
                {
                    Size = loadutFile.Size,
                    Hash = loadutFile.Hash,
                    EntityId = loadutFile.Id,
                };
                sourceItemType = LoadoutSourceItemType.Loadout;
            }
            else if (loadoutItem.TryGetAsDeletedFile(out var deletedFile))
            {
                sourceItem = new SyncNodePart
                {
                    Size = Size.Zero,
                    Hash = Hash.Zero,
                    EntityId = deletedFile.Id,
                };
                sourceItemType = LoadoutSourceItemType.Deleted;
            }
            else
            {
                throw new NotSupportedException("Only files and deleted files are supported");
            }
            
            ref var existing = ref CollectionsMarshal.GetValueRefOrAddDefault(results, loadoutItem.TargetPath, out var exists);
            if (!exists)
            {
                existing = new SyncTreeNode
                {
                    Loadout = sourceItem,
                    SourceItemType = sourceItemType,
                };
            }
            else
            {
                if (ShouldWin(targetPath, existing.Loadout, sourceItem))
                {
                    existing.Loadout = sourceItem;
                }
            }
        }
        
        // Remove deleted files. I'm not super happy with this requiring a full scan of
        // the loadout, but we have to somehow mark the deleted files and then delete them. 
        // And we can't modify the dictionary while iterating over it.
        List<GamePath> deletedFiles = [];
        foreach (var (key, value) in results)
        {
            if (value.SourceItemType == LoadoutSourceItemType.Deleted) 
                deletedFiles.Add(key);
        }
        foreach (var file in deletedFiles)
        {
            results.Remove(file);
        }
        
        return MergeStates(currentState, previousState, results);
    }

    /// <summary>
    /// For a given loadout, get the disabled groups, also any groups in the disabled groups.
    /// </summary>
    private HashSet<EntityId> GetDisabledGroups(Loadout.ReadOnly loadout)
    {
        var disabledGroups = new HashSet<EntityId>();
        var firstLevelDisabledGroups = loadout.Db
            .Datoms((LoadoutItem.LoadoutId, loadout.Id), (LoadoutItem.Disabled, Null.Instance));
        foreach (var disabledGroup in firstLevelDisabledGroups)
        {
            disabledGroups.Add(disabledGroup);
            MarkChildren(loadout.Db, disabledGroup, disabledGroups);
        }

        return disabledGroups;
        
        void MarkChildren(IDb loadoutDb, EntityId currentItem, HashSet<EntityId> disabledSet)
        {
            foreach (var child in loadoutDb.Datoms((LoadoutItem.Parent, currentItem), (LoadoutItemGroup.Group, Null.Instance)))
            {
                disabledSet.Add(child);
                MarkChildren(loadoutDb, child, disabledSet);
            }
        }
    }

    public IEnumerable<LoadoutSourceItem> GetNormalGameState(IDb referenceDb, Loadout.ReadOnly loadout)
    {
        foreach (var item in _fileHashService.GetGameFiles(referenceDb, loadout.InstallationInstance, loadout.LocatorIds.ToArray()))
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
    public Dictionary<GamePath, SyncTreeNode> MergeStates(IEnumerable<PathPartPair> currentState, IEnumerable<PathPartPair> previousTree, Dictionary<GamePath, SyncTreeNode> loadoutItems)
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
                existing = new SyncTreeNode
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
                existing = new SyncTreeNode
                {
                    Disk = node.Part,
                };
            }
        }
        return loadoutItems;
    }

    /// <summary>
    /// Returns true if the file and all its parents are not disabled.
    /// </summary>
    private static bool FileIsEnabled(LoadoutItem.ReadOnly arg)
    {
        return !arg.GetThisAndParents().Any(f => f.Contains(LoadoutItem.Disabled));
    }

    /// <inheritdoc />
    public async Task<Dictionary<GamePath, SyncTreeNode>> BuildSyncTree(Loadout.ReadOnly loadout)
    {
        var metadata = await ReindexState(loadout.InstallationInstance, Connection);
        var previouslyApplied = loadout.Installation.GetLastAppliedDiskState();
        return BuildSyncTree(DiskStateToPathPartPair(metadata.DiskStateEntries), DiskStateToPathPartPair(previouslyApplied), loadout);
    }

    /// <summary>
    /// Converts Mnemonic db disk state entries to path part pairs.
    /// </summary>
    /// <param name="entries"></param>
    /// <returns></returns>
    private IEnumerable<PathPartPair> DiskStateToPathPartPair(IEnumerable<DiskStateEntry.ReadOnly> entries)
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
                },
            };
        }
    }

    /// <inheritdoc />
    public Dictionary<GamePath, SyncTreeNode> ProcessSyncTree(Dictionary<GamePath, SyncTreeNode> tree)
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
                pathIsIgnored: IsIgnoredBackupPath(path));


            item.Signature = signature;
            item.Actions = ActionMapping.MapActions(signature);
        }

        return tree;
    }

    /// <inheritdoc />
    public async Task<Loadout.ReadOnly> RunGroupings(Dictionary<GamePath, SyncTreeNode> syncTree, Loadout.ReadOnly loadout)
    {
        using var tx = Connection.BeginTransaction();
        var gameMetadataId = loadout.InstallationInstance.GameMetadataId;
        var register = loadout.InstallationInstance.LocationsRegister;
        
        foreach (var action in ActionsInOrder)
        {
            switch (action)
            {
                case Actions.DoNothing:
                    break;

                case Actions.BackupFile:
                    await ActionBackupNewFiles(loadout.InstallationInstance, syncTree);
                    break;

                case Actions.IngestFromDisk:
                    await ActionIngestFromDisk(nodesForAction, loadout, tx);
                    break;

                case Actions.DeleteFromDisk:
                    ActionDeleteFromDisk(syncTree, register, tx, gameMetadataId);
                    break;

                case Actions.ExtractToDisk:
                    await ActionExtractToDisk(syncTree, register, tx, gameMetadataId);
                    break;

                case Actions.AddReifiedDelete:
                    ActionAddReifiedDelete(nodesForAction, loadout, tx);
                    break;

                case Actions.WarnOfUnableToExtract:
                    WarnOfUnableToExtract(nodesForAction);
                    break;

                case Actions.WarnOfConflict:
                    WarnOfConflict(nodesForAction);
                    break;

                default:
                    throw new InvalidOperationException($"Unknown action: {action}");
            }
        }

        tx.Add(gameMetadataId, GameInstallMetadata.LastSyncedLoadout, loadout.Id);
        tx.Add(gameMetadataId, GameInstallMetadata.LastSyncedLoadoutTransaction, EntityId.From(tx.ThisTxId.Value));
        tx.Add(gameMetadataId, GameInstallMetadata.LastScannedDiskStateTransaction, EntityId.From(tx.ThisTxId.Value));
        tx.Add(loadout.Id, Loadout.LastAppliedDateTime, DateTime.UtcNow);
        await tx.Commit();

        loadout = loadout.Rebase();
        var newState = loadout.Installation.DiskStateEntries;

        // Clean up empty directories
        var deletedFiles = groupings[Actions.DeleteFromDisk];
        if (deletedFiles.Count > 0)
        {
            CleanDirectories(deletedFiles.Select(f => f.Path), newState, loadout.InstallationInstance);
        }

        return loadout;
    }
    
    /// <inheritdoc />
    public async Task RunGroupings(Dictionary<GamePath, SyncTreeNode> tree, SyncActionGroupings<SyncTreeNode> groupings, GameInstallation gameInstallation)
    {
        using var tx = Connection.BeginTransaction();
        var gameMetadataId = gameInstallation.GameMetadataId;
        var gameMetadata = GameInstallMetadata.Load(Connection.Db, gameMetadataId);
        var register = gameInstallation.LocationsRegister;

        foreach (var action in ActionsInOrder)
        {
            var items = groupings[action];
            if (items.Count == 0)
                continue;

            switch (action)
            {
                case Actions.DoNothing:
                    break;

                case Actions.BackupFile:
                    await ActionBackupFiles(groupings, gameInstallation);
                    break;

                case Actions.IngestFromDisk:
                    throw new InvalidOperationException("Cannot ingest files from disk when not in a loadout context");

                case Actions.DeleteFromDisk:
                    ActionDeleteFromDisk(groupings, register, tx);
                    break;

                case Actions.ExtractToDisk:
                    await ActionExtractToDisk(groupings, register, tx,
                        gameMetadataId
                    );
                    break;

                case Actions.AddReifiedDelete:
                    throw new InvalidOperationException("Cannot add reified deletes when not in a loadout context");

                case Actions.WarnOfUnableToExtract:
                    WarnOfUnableToExtract(groupings);
                    break;

                case Actions.WarnOfConflict:
                    WarnOfConflict(groupings);
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

        await tx.Commit();
    }

    private void WarnOfConflict(SyncActionGroupings<SyncTreeNode> groupings)
    {
        var conflicts = groupings[Actions.WarnOfConflict];
        _logger.LogWarning("Conflict detected in {Count} files", conflicts.Count);

        foreach (var item in conflicts)
        {
            _logger.LogWarning("Conflict in {Path}", item.Path);
        }
    }

    private void WarnOfUnableToExtract(SyncActionGroupings<SyncTreeNode> groupings)
    {
        var unableToExtract = groupings[Actions.WarnOfUnableToExtract];
        _logger.LogWarning("Unable to extract {Count} files", unableToExtract.Count);

        foreach (var item in unableToExtract)
        {
            _logger.LogWarning("Unable to extract {Path}", item.Path);
        }
    }

    private void ActionAddReifiedDelete(SyncActionGroupings<SyncTreeNode> groupings, Loadout.ReadOnly loadout, ITransaction tx)
    {
        var toAddDelete = groupings[Actions.AddReifiedDelete];
        _logger.LogDebug("Adding {Count} reified deletes", toAddDelete.Count);

        var overridesGroup = GetOrCreateOverridesGroup(tx, loadout);

        foreach (var item in toAddDelete)
        {
            var delete = new DeletedFile.New(tx, out var id)
            {
                Reason = "Reified delete",
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(tx, id)
                {
                    TargetPath = item.Path.ToGamePathParentTuple(loadout.Id),
                    LoadoutItem = new LoadoutItem.New(tx, id)
                    {
                        Name = item.Path.FileName,
                        ParentId = overridesGroup.Value,
                        LoadoutId = loadout.Id,
                    },
                },
            };
        }
    }

    private async Task ActionExtractToDisk(Dictionary<GamePath, SyncTreeNode> groupings, IGameLocationsRegister register, ITransaction tx, EntityId gameMetadataId)
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
        _logger.LogDebug("Extracting {Count} files to disk", toExtract.Count);
        
        if (toExtract.Count > 0)
        {
            await _fileStore.ExtractFiles(toExtract, CancellationToken.None);

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
    }

    private void ActionDeleteFromDisk(Dictionary<GamePath, SyncTreeNode> groupings, IGameLocationsRegister register, ITransaction tx, GameInstallMetadataId gameMetadataId)
    {
        // Delete files from disk
        foreach (var (path, node) in groupings)
        {
            if (!node.Actions.HasFlag(Actions.DeleteFromDisk))
                continue;
            var gamePath = register.GetResolvedPath(path);
            gamePath.Delete();

            // Don't delete the entry if we're just going to replace it
            if (!node.Actions.HasFlag(Actions.ExtractToDisk))
            {
                var id = node.Disk.EntityId;
                throw new NotImplementedException();
                tx.Retract(id, DiskStateEntry.Path, ((EntityId)gameMetadataId, path.LocationId, path.Path));
                tx.Retract(id, DiskStateEntry.Hash, node.Disk.Hash);
                tx.Retract(id, DiskStateEntry.Size, node.Disk.Size);
                tx.Retract(id, DiskStateEntry.LastModified, new DateTimeOffset(node.Disk.LastModifiedTicks, TimeSpan.Zero));
                tx.Retract(id, DiskStateEntry.Game, (EntityId)gameMetadataId);
            }
        }
    }

    private async Task ActionBackupFiles(Dictionary<GamePath, SyncTreeNode> grouping, GameInstallation gameInstallation)
    {
        _logger.LogDebug("Backing up {Count} files", grouping.Count);
        
        List<(GamePath Path, Hash Hash, Size Size)> files = [];

        foreach (var (path, node) in grouping)
        {
            
        }

        await BackupNewFiles(gameInstallation, grouping.Select(item =>
                (item.Path, item.Disk.Value.Hash, item.Disk.Value.Size)
            )
        );
    }

    public record struct AddedEntry
    {
        public required LoadoutItem.New LoadoutItem { get; init; }
        public required LoadoutItemWithTargetPath.New LoadoutItemWithTargetPath { get; init; }
        public required LoadoutFile.New LoadoutFileEntry { get; init; }
    }

    private async Task ActionIngestFromDisk(SyncActionGroupings<SyncTreeNode> groupings, Loadout.ReadOnly loadout, ITransaction tx)
    {
        var toIngest = groupings[Actions.IngestFromDisk];
        _logger.LogDebug("Ingesting {Count} files", toIngest.Count);
        var overridesMod = GetOrCreateOverridesGroup(tx, loadout);

        var added = new List<AddedEntry>();

        foreach (var file in toIngest)
        {
            // If the file is already in the loadout, we just need to update entry's hash and size
            if (file.LoadoutFileId.HasValue)
            {
                var prevLoadoutFile = LoadoutItemWithTargetPath.Load(Connection.Db, file.LoadoutFileId.Value);
                if (prevLoadoutFile.IsValid())
                {
                    tx.Add(prevLoadoutFile.Id, LoadoutFile.Hash, file.Disk.Value.Hash);
                    tx.Add(prevLoadoutFile.Id, LoadoutFile.Size, file.Disk.Value.Size);
                    tx.Add(file.Disk.Value.Id, DiskStateEntry.LastModified, file.Disk.Value.LastModified);
                    continue;
                }
            }
            
            // If the file is not in the loadout, we need to add it
            var id = tx.TempId();
            var loadoutItem = new LoadoutItem.New(tx, id)
            {
                ParentId = overridesMod.Value,
                LoadoutId = loadout.Id,
                Name = file.Path.FileName,
            };
            var loadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(tx, id)
            {
                LoadoutItem = loadoutItem,
                TargetPath = file.Path.ToGamePathParentTuple(loadout.Id),
            };

            var loadoutFile = new LoadoutFile.New(tx, id)
            {
                LoadoutItemWithTargetPath = loadoutItemWithTargetPath,
                Hash = file.Disk.Value.Hash,
                Size = file.Disk.Value.Size,
            };

            added.Add(new AddedEntry
                {
                    LoadoutItem = loadoutItem,
                    LoadoutItemWithTargetPath = loadoutItemWithTargetPath,
                    LoadoutFileEntry = loadoutFile,
                }
            );
            tx.Add(file.Disk.Value.Id, DiskStateEntry.LastModified, file.Disk.Value.LastModified);
        }

        if (added.Count > 0)
        {
            await MoveNewFilesToMods(loadout, added, tx);
        }
    }

    /// <inheritdoc />
    public virtual async Task<Loadout.ReadOnly> Synchronize(Loadout.ReadOnly loadout)
    {
        // If we are swapping loadouts, then we need to synchronize the previous loadout first to ingest
        // any changes, then we can apply the new loadout.
        if (GameInstallMetadata.LastSyncedLoadout.TryGetValue(loadout.Installation, out var lastAppliedId) && lastAppliedId != loadout.Id)
        {
            var prevLoadout = Loadout.Load(loadout.Db, lastAppliedId);
            if (prevLoadout.IsValid())
            {
                await Synchronize(prevLoadout);
                await DeactivateCurrentLoadout(loadout.InstallationInstance);
                await ActivateLoadout(loadout);
                return loadout.Rebase();
            }
        }

        var tree = await BuildSyncTree(loadout);
        var groupings = ProcessSyncTree(tree);
        return await RunGroupings(tree, groupings, loadout);
    }

    public async Task<GameInstallMetadata.ReadOnly> RescanGameFiles(GameInstallation gameInstallation)
    {
        // Make sure the file hashes are up to date
        await _fileHashService.GetFileHashesDb();
        return await ReindexState(gameInstallation, Connection);
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
    /// Given a list of files with duplicate game paths, select the winning file that will be applied to disk.
    /// </summary>
    protected virtual LoadoutItemWithTargetPath.ReadOnly SelectWinningFile(IEnumerable<LoadoutItemWithTargetPath.ReadOnly> files)
    {
        return files.MaxBy(GetPriority);

        // Placeholder for a more advanced selection algorithm
        long GetPriority(LoadoutItemWithTargetPath.ReadOnly item)
        {
            foreach (var parent in item.AsLoadoutItem().GetThisAndParents())
            {
                if (!parent.TryGetAsLoadoutItemGroup(out var group))
                    continue;
                
                // GameFiles always lose
                if (group.TryGetAsLoadoutGameFilesGroup(out var gameFilesGroup))
                    return 0;
                
                // Overrides should always win
                if (group.TryGetAsLoadoutOverridesGroup(out var overridesGroup))
                    return long.MaxValue;
                
                // Return a placeholder priority based on creation time of the LoadoutGroup, newest wins.
                // This allows for some degree of control and predictability in the selection process.
                return group.GetCreatedAt().ToUnixTimeSeconds();
            }
            
            // Should not happen
            Debug.Assert(false, "LoadoutItem is not part of a LoadoutItemGroup");
            return 0;
        }
    }
    
    /// <summary>
    /// Return true if the replacement should win over the existing item.
    /// </summary>
    protected virtual bool ShouldWin(in GamePath path, in SyncNodePart existing, in SyncNodePart replacement)
    {
        return true;
    }

    /// <summary>
    /// When new files are added to the loadout from disk, this method will be called to move the files from the override mod
    /// into any other mod they may belong to.
    /// </summary>
    protected virtual ValueTask MoveNewFilesToMods(Loadout.ReadOnly loadout, IEnumerable<AddedEntry> newFiles, ITransaction tx)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public FileDiffTree LoadoutToDiskDiff(Loadout.ReadOnly loadout, DiskState diskState)
    {
        var syncTree = BuildSyncTree(diskState, diskState, loadout);
        // Process the sync tree to get the actions populated in the nodes
        ProcessSyncTree(syncTree);

        List<DiskDiffEntry> diffs = new();

        foreach (var (_, node) in syncTree)
        {
            var syncNode = node;
            var actions = syncNode.Actions;
            
            if (actions.HasFlag(Actions.DoNothing))
            {
                var entry = new DiskDiffEntry
                {
                    Hash = node.LoadoutFileHash.Value,
                    Size = node.LoadoutFileSize.Value,
                    ChangeType = FileChangeType.None,
                    GamePath = node.Path,
                };
                diffs.Add(entry);
            }
            else if (actions.HasFlag(Actions.ExtractToDisk))
            {
                var entry = new DiskDiffEntry
                {
                    Hash = node.LoadoutFileHash.Value,
                    Size = node.LoadoutFileSize.Value,
                    // If paired with a delete action, this is a modified file not a new one
                    ChangeType = actions.HasFlag(Actions.DeleteFromDisk) ? FileChangeType.Modified : FileChangeType.Added,
                    GamePath = node.Path,
                };
                diffs.Add(entry);
            }
            else if (actions.HasFlag(Actions.DeleteFromDisk))
            {
                var entry = new DiskDiffEntry
                {
                    Hash = node.Disk.Value.Hash,
                    Size = node.Disk.Value.Size,
                    ChangeType = FileChangeType.Removed,
                    GamePath = node.Path,
                };
                diffs.Add(entry);
            }
            else
            {
                // This really should become some sort of error state
                var entry = new DiskDiffEntry
                {
                    Hash = Hash.Zero,
                    Size = Size.Zero,
                    ChangeType = FileChangeType.None,
                    GamePath = node.Path,
                };
                diffs.Add(entry);
            }
        }

        return FileDiffTree.Create(diffs.Select(d => KeyValuePair.Create(d.GamePath, d)));
    }

    /// <summary>
    /// Backs up any new files in the loadout.
    ///
    /// </summary>
    public virtual async Task ActionBackupNewFiles(GameInstallation installation, Dictionary<GamePath, SyncTreeNode> files)
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
            }
        );

        // PERFORMANCE: We deduplicate above with the HaveFile call.
        await _fileStore.BackupFiles(archivedFiles, deduplicate: false);
    }

    private async Task<DiskState> GetOrCreateInitialDiskState(GameInstallation installation)
    {
        // Return any existing state
        var metadata = installation.GetMetadata(Connection);
        if (metadata.Contains(GameInstallMetadata.InitialDiskStateTransaction))
        {
            return metadata.DiskStateAsOf(metadata.InitialDiskStateTransaction);
        }

        // Or create a new one
        using var tx = Connection.BeginTransaction();
        await IndexNewState(installation, tx);
        tx.Add(metadata.Id, GameInstallMetadata.InitialDiskStateTransaction, EntityId.From(tx.ThisTxId.Value));
        tx.Add(metadata.Id, GameInstallMetadata.LastScannedDiskStateTransaction, EntityId.From(tx.ThisTxId.Value));
        await tx.Commit();

        // Rebase the metadata to the new transaction
        metadata = metadata.Rebase();

        // Return the new state
        return metadata.DiskStateAsOf(metadata.InitialDiskStateTransaction);
    }
    
    /// <summary>
    /// Reindex the state of the game, running a transaction if changes are found
    /// </summary>
    private async Task<GameInstallMetadata.ReadOnly> ReindexState(GameInstallation installation, IConnection connection)
    {
        var originalMetadata = installation.GetMetadata(connection);
        using var tx = connection.BeginTransaction();

        // Index the state
        var changed = await ReindexState(installation, connection, tx);
        
        if (!originalMetadata.Contains(GameInstallMetadata.InitialDiskStateTransaction))
        {
            // No initial state, so set this transaction as the initial state
            changed = true;
            tx.Add(originalMetadata.Id, GameInstallMetadata.InitialDiskStateTransaction, EntityId.From(TxId.Tmp.Value));
        }
        
        if (changed)
        {
            await tx.Commit();
        }
        
        return GameInstallMetadata.Load(connection.Db, installation.GameMetadataId);
    }
    
    /// <summary>
    /// Reindex the state of the game
    /// </summary>
    public async Task<bool> ReindexState(GameInstallation installation, IConnection connection, ITransaction tx)
    {
        var seen = new HashSet<GamePath>();
        var metadata = GameInstallMetadata.Load(connection.Db, installation.GameMetadataId);
        var inState = metadata.DiskStateEntries.ToDictionary(e => (GamePath)e.Path);
        var changes = false;
        
        foreach (var location in installation.LocationsRegister.GetTopLevelLocations())
        {
            if (!location.Value.DirectoryExists())
                continue;

            await Parallel.ForEachAsync(location.Value.EnumerateFiles(), async (file, token) =>
                {
                    {
                        var gamePath = installation.LocationsRegister.ToGamePath(file);
                        
                        if (IsIgnoredPath(gamePath))
                            return;
                        
                        lock (seen)
                        {
                            seen.Add(gamePath);
                        }

                        if (inState.TryGetValue(gamePath, out var entry))
                        {
                            var fileInfo = file.FileInfo;
                            var writeTimeUtc = new DateTimeOffset(fileInfo.LastWriteTimeUtc);

                            // If the files don't match, update the entry
                            if (writeTimeUtc != entry.LastModified || fileInfo.Size != entry.Size)
                            {
                                var newHash = await file.XxHash3Async();
                                tx.Add(entry.Id, DiskStateEntry.Size, fileInfo.Size);
                                tx.Add(entry.Id, DiskStateEntry.Hash, newHash);
                                tx.Add(entry.Id, DiskStateEntry.LastModified, writeTimeUtc);
                                changes = true;
                            }
                        }
                        else
                        {
                            // No previous entry found, so create a new one
                            var newHash = await file.XxHash3Async(token: token);
                            _ = new DiskStateEntry.New(tx, tx.TempId(DiskStateEntry.EntryPartition))
                            {
                                Path = gamePath.ToGamePathParentTuple(metadata.Id),
                                Hash = newHash,
                                Size = file.FileInfo.Size,
                                LastModified = file.FileInfo.LastWriteTimeUtc,
                                GameId = metadata.Id,
                            };
                            changes = true;
                        }
                    }
                }
            );
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
            tx.Add(metadata.Id, GameInstallMetadata.LastScannedDiskStateTransaction, EntityId.From(TxId.Tmp.Value));
        
        return changes;
    }
    

    /// <summary>
    /// Index the game state and create the initial disk state
    /// </summary>
    public async Task IndexNewState(GameInstallation installation, ITransaction tx)
    {
        var metaDataId = installation.GameMetadataId;

        var hashDb = await _fileHashService.GetFileHashesDb();
        
        foreach (var location in installation.LocationsRegister.GetTopLevelLocations())
        {
            if (!location.Value.DirectoryExists())
                continue;

            await Parallel.ForEachAsync(location.Value.EnumerateFiles(), async (file, token) =>
                {
                    var (gamePath, newHash) = await HashGameFile(hashDb, installation, file, token);
                    _ = new DiskStateEntry.New(tx, tx.TempId(DiskStateEntry.EntryPartition))
                    {
                        Path = gamePath.ToGamePathParentTuple(metaDataId),
                        Hash = newHash,
                        Size = file.FileInfo.Size,
                        LastModified = file.FileInfo.LastWriteTimeUtc,
                        GameId = metaDataId,
                    };
                }
            );
        }
    }

    /// <summary>
    /// Hash a file but first check the hash database for a matching size and minimal hash, and use that to reduce
    /// the amount of work needed to produce the hash.
    /// </summary>
    private static async ValueTask<(GamePath gamePath, Hash newHash)> HashGameFile(IDb hashDb, GameInstallation installation, AbsolutePath file, CancellationToken token)
    {
        var gamePath = installation.LocationsRegister.ToGamePath(file);
        
        // It's cheapest to look at the size first, if there's no matching size then we don't do a minimal hash. 
        // File sizes are actually fairly unique. Of-course they collide a lot, but since most games have different
        // types of file and pack their assets in archives, the number of collisions are fairly low. Using a path
        // first is more expensive due to it being a insensitive string comparsion, and we'd have to hash the file
        // on disk to get the minimal hash. 
        // So we do size -> minimal hash -> path (path being uses mostly as a doublecheck at the end).
        var relation = HashRelation.FindBySize(hashDb, file.FileInfo.Size);
        if (relation.Count != 0)
        {
            // We have a size match, so minimal hash it, and see if we have a match
            var minimalHash = await MultiHasher.MinimalHash(file, token);
            foreach (var matchingHash in HashRelation.FindByMinimalHash(hashDb, minimalHash))
            {
                // Match on the relative path as well as the minimal hash
                foreach (var path in matchingHash.Paths)
                {
                    if (path.Path == gamePath.Path)
                        return (gamePath, matchingHash.XxHash3);
                }
                
            }
        }
        
        // No other matches, so do a full hash
        var newHash = await file.XxHash3Async(token: token);
        return (gamePath, newHash);
    }

    /// <inheritdoc />
    public virtual IJobTask<CreateLoadoutJob, Loadout.ReadOnly> CreateLoadout(GameInstallation installation, string? suggestedName = null)
    {

        return _jobMonitor.Begin(new CreateLoadoutJob(installation), async ctx =>
            {
                await _fileHashService.GetFileHashesDb();
                //var initialState = await GetOrCreateInitialDiskState(installation);
                var existingLoadoutNames = Loadout.All(Connection.Db)
                    .Where(l => l.IsVisible()
                                && l.InstallationInstance.LocationsRegister[LocationId.Game]
                                == installation.LocationsRegister[LocationId.Game]
                    )
                    .Select(l => l.ShortName)
                    .ToArray();

                var isOnlyLoadout = existingLoadoutNames.Length == 0;

                var shortName = LoadoutNameProvider.GetNewShortName(existingLoadoutNames);

                using var tx = Connection.BeginTransaction();

                List<string> locatorMetadata = [];
                if (installation.LocatorResultMetadata != null)
                {
                    locatorMetadata.Add(installation.LocatorResultMetadata.ToCommonString());
                }

                var loadout = new Loadout.New(tx)
                {
                    Name = suggestedName ?? "Loadout " + shortName,
                    ShortName = shortName,
                    InstallationId = installation.GameMetadataId,
                    Revision = 0,
                    LoadoutKind = LoadoutKind.Default,
                    LocatorIds = locatorMetadata,
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

                // If this is the only loadout, activate it
                if (isOnlyLoadout)
                {
                    await ActivateLoadout(remappedLoadout.Id);
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
        
        await ResetToOriginalGameState(installation, []);
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
        var reindexed = await ReindexState(loadout.InstallationInstance, Connection);
        
        var tree = BuildSyncTree(reindexed.DiskStateEntries, reindexed.DiskStateEntries, loadout);
        var groupings = ProcessSyncTree(tree);
        await RunGroupings(tree, groupings, loadout);
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
    public async Task UnManage(GameInstallation installation, bool runGc = true)
    {
        await _jobMonitor.Begin(new UnmanageGameJob(installation), async ctx =>
            {

                var metadata = installation.GetMetadata(Connection);

                if (GetCurrentlyActiveLoadout(installation).HasValue)
                    await DeactivateCurrentLoadout(installation);

                await ctx.YieldAsync();
                foreach (var loadout in metadata.Loadouts)
                {
                    _logger.LogInformation("Deleting loadout {Loadout} - {ShortName}", loadout.Name, loadout.ShortName);
                    await ctx.YieldAsync();
                    await DeleteLoadout(loadout, GarbageCollectorRunMode.DoNotRun);
                }

                if (runGc)
                    _garbageCollectorRunner.Run();
                return installation;
            }
        );
    }

    /// <inheritdoc />
    public virtual bool IsIgnoredBackupPath(GamePath path)
    {
        return false;
    }

    /// <inheritdoc />
    public virtual bool IsIgnoredPath(GamePath path)
    {
        return false;
    }

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
        
        // Generate a new name and short name
        var newShortName = LoadoutNameProvider.GetNewShortName(Loadout.All(baseDb)
            .Where(l => l.IsVisible() && l.InstallationId == loadout.InstallationId)
            .Select(l => l.ShortName)
            .ToArray()
        );
        var newName = "Loadout " + newShortName;
        
        // Create a mapping of old entity ids to new (temp) entity ids
        Dictionary<EntityId, EntityId> entityIdList = new();
        var remapFn = RemapFn;
        
        using var tx = Connection.BeginTransaction();

        // Add the loadout
        entityIdList[loadout.Id] = tx.TempId();
        
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
                // Rename the loadout
                if (datom.A == nameId)
                {
                    tx.Add(newId, Loadout.Name, newName);
                    continue;
                }
                if (datom.A == shortNameId)
                {
                    tx.Add(newId, Loadout.ShortName, newShortName);
                    continue;
                }

                // Make sure we have enough buffer space
                if (buffer.Length < datom.ValueSpan.Length)
                    buffer = System.GC.AllocateUninitializedArray<byte>(datom.ValueSpan.Length);
                
                // Copy the value over
                datom.ValueSpan.CopyTo(buffer.Span);
                
                // Create the new datom and reference the copied value
                var prefix = new KeyPrefix(newId, datom.A, TxId.Tmp, false, datom.Prefix.ValueTag);
                var newDatom = new Datom(prefix, buffer[..datom.ValueSpan.Length]);
                
                // Remap any entity ids in the value
                datom.Prefix.ValueTag.Remap(buffer[..datom.ValueSpan.Length].Span, remapFn);
                
                // Add the new datom
                tx.Add(newDatom);
            }
        }

        var result = await tx.Commit();

        return Loadout.Load(Connection.Db, result[entityIdList[loadout.Id]]);
        
        // Local function to remap entity ids in the format Attribute.Remap wants
        EntityId RemapFn(EntityId entityId)
        {
            return entityIdList.GetValueOrDefault(entityId, entityId);
        }
    }


    /// <inheritdoc />
    public async Task DeleteLoadout(LoadoutId loadoutId, GarbageCollectorRunMode gcRunMode = GarbageCollectorRunMode.RunAsyncInBackground)
    {
        var loadout = Loadout.Load(Connection.Db, loadoutId);
        var metadata = GameInstallMetadata.Load(Connection.Db, loadout.InstallationInstance.GameMetadataId);
        if (GameInstallMetadata.LastSyncedLoadout.TryGetValue(metadata, out var lastAppliedLoadout) && lastAppliedLoadout == loadoutId.Value)
        {
            await DeactivateCurrentLoadout(loadout.InstallationInstance);
        }
        
        using var tx = Connection.BeginTransaction();
        tx.Delete(loadoutId, false);
        foreach (var item in loadout.Items)
        {
            tx.Delete(item.Id, false);
        }
        await tx.Commit();
        
        // Execute the garbage collector
        _garbageCollectorRunner.RunWithMode(gcRunMode);
    }
    
    
    /// <inheritdoc />
    public async Task ResetToOriginalGameState(GameInstallation installation, string[] commonIds)
    {
        var gameState = _fileHashService.GetGameFiles(_fileHashService.Current, installation, commonIds);
        throw new NotImplementedException();
        /*
        var metaData = await ReindexState(installation, Connection);
        if (!metaData.Contains(GameInstallMetadata.InitialDiskStateTransaction))
            throw new InvalidOperationException("No initial state transaction found for game");
        
        var currentState = metaData.DiskStateEntries;
        var initialState = metaData.DiskStateAsOf(metaData.InitialDiskStateTransaction);
        var prevState = metaData.GetLastAppliedDiskState();
        
        // Bit strange, but we're setting the "loadout" to the initial state here.
        // previous state is the last applied state. This then tells the sync system that we don't want integrate any disk changes into the loadout
        // but instead want a "hard reset" to a previous state. 
        
        var syncTree = BuildSyncTree(currentState, prevState, initialState);
        var groups = ProcessSyncTree(syncTree);

        await RunGroupings(syncTree, groups, installation);
        */
    }
}

#endregion
