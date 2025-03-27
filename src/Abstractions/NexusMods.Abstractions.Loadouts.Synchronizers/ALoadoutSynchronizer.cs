using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.GameLocators;
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
using NexusMods.Abstractions.NexusModsLibrary.Models;
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
 
    private readonly ScopedAsyncLock _lock = new();
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



    public Dictionary<GamePath, SyncNode> BuildSyncTree(IEnumerable<PathPartPair> currentState, IEnumerable<PathPartPair> previousState, Loadout.ReadOnly loadout)
    {
        var referenceDb = _fileHashService.Current;
        Dictionary<GamePath, SyncNode> syncTree = new();
        
        // Add in the game state
        foreach (var gameFile in GetNormalGameState(referenceDb, loadout))
        {
            syncTree.Add(gameFile.Path, new SyncNode
                {
                    Loadout = new SyncNodePart
                    {
                        Hash = gameFile.Hash,
                        Size = gameFile.Size,
                        LastModifiedTicks = 0,
                    },
                    SourceItemType = LoadoutSourceItemType.Game,
                }
            );
        }
        
        foreach (var loadoutItem in loadout.Items.OfTypeLoadoutItemWithTargetPath())
        {
            var targetPath = loadoutItem.TargetPath;
            // Ignore disabled Items
            if (!loadoutItem.AsLoadoutItem().IsEnabled())
                continue;

            SyncNodePart sourceItem;
            LoadoutSourceItemType sourceItemType;
            if (loadoutItem.TryGetAsLoadoutFile(out var loadutFile))
            {
                sourceItem = new SyncNodePart
                {
                    Size = loadutFile.Size,
                    Hash = loadutFile.Hash,
                    EntityId = loadutFile.Id,
                    LastModifiedTicks = 0,
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
                    LastModifiedTicks = 0,
                };
                sourceItemType = LoadoutSourceItemType.Deleted;
            }
            else
            {
                throw new NotSupportedException("Only files and deleted files are supported");
            }
            
            ref var existing = ref CollectionsMarshal.GetValueRefOrAddDefault(syncTree, loadoutItem.TargetPath, out var exists);
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
        
        MergeStates(currentState, previousState, syncTree);
        return syncTree;
    }

    public IEnumerable<LoadoutSourceItem> GetNormalGameState(IDb referenceDb, Loadout.ReadOnly loadout)
    {
        foreach (var item in _fileHashService.GetGameFiles((loadout.InstallationInstance.Store, loadout.LocatorIds.ToArray())))
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
        var metadata = await ReindexState(loadout.InstallationInstance, Connection);
        var previouslyApplied = loadout.Installation.GetLastAppliedDiskState();
        return BuildSyncTree(DiskStateToPathPartPair(metadata.DiskStateEntries), DiskStateToPathPartPair(previouslyApplied), loadout);
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
                pathIsIgnored: IsIgnoredBackupPath(path));


            item.Signature = signature;
            item.Actions = ActionMapping.MapActions(signature);
        }
    }

    /// <inheritdoc />
    public async Task<Loadout.ReadOnly> RunActions(Dictionary<GamePath, SyncNode> syncTree, Loadout.ReadOnly loadout)
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
                    await ActionBackupNewFiles(loadout.InstallationInstance, syncTree);
                    break;

                case Actions.IngestFromDisk:
                    ActionIngestFromDisk(syncTree, loadout, tx, ref overridesGroup);
                    break;

                case Actions.DeleteFromDisk:
                    ActionDeleteFromDisk(syncTree, register, tx, gameMetadataId, foldersWithDeletedFiles);
                    break;

                case Actions.ExtractToDisk:
                    await ActionExtractToDisk(syncTree, register, tx, gameMetadataId);
                    break;

                case Actions.AddReifiedDelete:
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

        loadout = await ReprocessOverrides(loadout);

        return loadout;
    }

    private async ValueTask<Loadout.ReadOnly> ReprocessOverrides(Loadout.ReadOnly loadout)
    {
        loadout = await ReprocessGameUpdates(loadout);
        return loadout;
    }

    /// <summary>
    /// When enough new files show up in overrides, we may need to reprocess them and change out the game version
    /// to reflect the new files. This will happen when a game store updates the game files, overwriting the existing
    /// game files.
    /// </summary>
    private async ValueTask<Loadout.ReadOnly> ReprocessGameUpdates(Loadout.ReadOnly loadout)
    {
        var diskState = loadout.Installation.DiskStateEntries
            .Select(part => ((GamePath)part.Path, part.Hash));
        
        var suggestedVersionDefinition = _fileHashService.SuggestVersionData(loadout.InstallationInstance, diskState);
        if (!suggestedVersionDefinition.HasValue)
            return loadout;
        
        var newLocatorIds = suggestedVersionDefinition.Value.LocatorIds;

        var locatorAdditions = loadout.LocatorIds.Except(newLocatorIds).Count();
        var locatorRemovals = newLocatorIds.Except(loadout.LocatorIds).Count();

        // No reason to change the loadout if the version is the same
        if (locatorRemovals == 0 && locatorAdditions == 0)
            return loadout;

        // Make a lookup set of the new files
        var versionFiles = _fileHashService
            .GetGameFiles((loadout.InstallationInstance.Store, newLocatorIds))
            .Select(file => file.Path)
            .ToHashSet();

        // Find all files in the overrides that match a path in the new files
        var toDelete = from grp in loadout.Items.OfTypeLoadoutItemGroup().OfTypeLoadoutOverridesGroup()
            from item in grp.AsLoadoutItemGroup().Children.OfTypeLoadoutItemWithTargetPath()
            let path = (GamePath)item.TargetPath
            where versionFiles.Contains(path)
            select item;

        using var tx = Connection.BeginTransaction();
        
        // Delete all the matching override files
        foreach (var file in toDelete)
        {
            tx.Delete(file, false);
        }
        
        
        
        // Update the version and locator ids
        tx.Add(loadout, Loadout.GameVersion, suggestedVersionDefinition.Value.VanityVersion);
        foreach (var id in loadout.LocatorIds) 
            tx.Retract(loadout, Loadout.LocatorIds, id);
        foreach (var id in newLocatorIds)
            tx.Add(loadout, Loadout.LocatorIds, id);
        
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

                case Actions.IngestFromDisk:
                    #if DEBUG
                    if (syncTree.Any(n => n.Value.Actions.HasFlag(Actions.IngestFromDisk)))
                        throw new InvalidOperationException("Cannot ingest files from disk when not in a loadout context");
                    #endif
                    break;

                case Actions.DeleteFromDisk:
                    ActionDeleteFromDisk(syncTree, register, tx, gameInstallation.GameMetadataId, foldersWithDeletedFiles);
                    break;

                case Actions.ExtractToDisk:
                    await ActionExtractToDisk(syncTree, register, tx, gameMetadataId);
                    break;

                case Actions.AddReifiedDelete:
                    #if DEBUG
                    if (syncTree.Any(n => n.Value.Actions.HasFlag(Actions.AddReifiedDelete)))
                        throw new InvalidOperationException("Cannot add reified deletes when not in a loadout context");
                    #endif
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
            _logger.LogWarning("Conflict in {Path}", path);
        }
    }

    private void WarnOfUnableToExtract(Dictionary<GamePath, SyncNode> groupings)
    {
        foreach (var (path, node) in groupings)
        {
            if (!node.Actions.HasFlag(Actions.WarnOfUnableToExtract))
                continue;
            _logger.LogWarning("Unable to extract {Path}", path);
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
                    tx.Delete(match, false);
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

    private async Task ActionExtractToDisk(Dictionary<GamePath, SyncNode> groupings, IGameLocationsRegister register, ITransaction tx, EntityId gameMetadataId)
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

    private void ActionDeleteFromDisk(
        Dictionary<GamePath, SyncNode> groupings,
        IGameLocationsRegister register,
        ITransaction tx,
        GameInstallMetadataId gameMetadataId,
        HashSet<GamePath> foldersWithDeletedFiles)
    {
        // Delete files from disk
        foreach (var (path, node) in groupings)
        {
            if (!node.Actions.HasFlag(Actions.DeleteFromDisk))
                continue;
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
    public virtual async Task<Loadout.ReadOnly> Synchronize(Loadout.ReadOnly loadout)
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

        var tree = await BuildSyncTree(loadout);
        ProcessSyncTree(tree);
        return await RunActions(tree, loadout);
    }

    public async Task<GameInstallMetadata.ReadOnly> RescanFiles(GameInstallation gameInstallation)
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
;
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
    public bool ShouldSynchronize(Loadout.ReadOnly loadout, DiskState previousDiskState, DiskState lastScannedDiskState)
    {
        var syncTree = BuildSyncTree(DiskStateToPathPartPair(lastScannedDiskState), DiskStateToPathPartPair(previousDiskState), loadout);
        // Process the sync tree to get the actions populated in the nodes
        ProcessSyncTree(syncTree);
        
        return syncTree.Any(n => n.Value.Actions != Actions.DoNothing);
    }
    
    /// <inheritdoc />
    public FileDiffTree LoadoutToDiskDiff(Loadout.ReadOnly loadout, DiskState previousDiskState, DiskState lastScannedDiskState)
    {
        var syncTree = BuildSyncTree(DiskStateToPathPartPair(lastScannedDiskState), DiskStateToPathPartPair(previousDiskState), loadout);
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
    
    /// <summary>
    /// Reindex the state of the game, running a transaction if changes are found
    /// </summary>
    private async Task<GameInstallMetadata.ReadOnly> ReindexState(GameInstallation installation, IConnection connection)
    {        
        using var _ = await _lock.LockAsync();
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
        
        
        var hashDb = await _fileHashService.GetFileHashesDb();
        
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
                                var newHash = await MaybeHashFile(hashDb, gamePath, file, fileInfo, token);
                                tx.Add(entry.Id, DiskStateEntry.Size, fileInfo.Size);
                                tx.Add(entry.Id, DiskStateEntry.Hash, newHash);
                                tx.Add(entry.Id, DiskStateEntry.LastModified, writeTimeUtc);
                                changes = true;
                            }
                        }
                        else
                        {
                            // No previous entry found, so create a new one
                            var newHash = await MaybeHashFile(hashDb, gamePath, file, file.FileInfo, token);
                            var diskState = new DiskStateEntry.New(tx, tx.TempId(DiskStateEntry.EntryPartition))
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

    private async ValueTask<Hash> MaybeHashFile(IDb hashDb, GamePath gamePath, AbsolutePath file, IFileEntry fileInfo, CancellationToken token)
    {
        Hash? diskMinimalHash = null;
        
        // Look for all known files that match the path
        foreach (var matchingPath in PathHashRelation.FindByPath(hashDb, gamePath.Path))
        {
            // Make sure the size matches
            var hash = matchingPath.Hash;
            if (hash.Size.Value != fileInfo.Size)
                continue;
            
            // If the minimal hash matches, then we can use the xxHash3 hash
            diskMinimalHash ??= await MultiHasher.MinimalHash(file, token);

            if (hash.MinimalHash == diskMinimalHash)
                return hash.XxHash3;
        }
        
        // If we didn't find a match, then we need to hash the file
        return await file.XxHash3Async(token: token);
    }

    /// <inheritdoc />
    public virtual IJobTask<CreateLoadoutJob, Loadout.ReadOnly> CreateLoadout(GameInstallation installation, string? suggestedName = null)
    {

        return _jobMonitor.Begin(new CreateLoadoutJob(installation), async ctx =>
            {
                // Prime the hash database to make sure it's loaded
                await _fileHashService.GetFileHashesDb();
                
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

                List<LocatorId> locatorIds = [];
                if (installation.LocatorResultMetadata != null)
                {
                    locatorIds.AddRange(installation.LocatorResultMetadata.ToLocatorIds());
                }

                if (!_fileHashService.TryGetVanityVersion((installation.Store, locatorIds.ToArray()), out var version))
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

                // Commit the transaction as of this point the loadout is live
                var result = await tx.Commit();

                // Remap the id
                var remappedLoadout = result.Remap(loadout);

                // If there is no currently synced loadout, then we can ingest the game folder
                if (!remappedLoadout.Installation.Contains(GameInstallMetadata.LastSyncedLoadout))
                {
                    remappedLoadout = await Synchronize(remappedLoadout);
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

        var commonIds = installation.LocatorResultMetadata?.ToLocatorIds().ToArray() ?? [];
        await ResetToOriginalGameState(installation, commonIds);
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

    public async Task ResetToOriginalGameState(GameInstallation installation, LocatorId[] locatorIds)
    {
        var gameState = _fileHashService.GetGameFiles((installation.Store, locatorIds));
        var metaData = await ReindexState(installation, Connection);

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
