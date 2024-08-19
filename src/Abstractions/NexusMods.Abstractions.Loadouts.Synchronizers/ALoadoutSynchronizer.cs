using System.Collections.Concurrent;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.Loadouts.Sorting;
using NexusMods.Abstractions.Games.Trees;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Synchronizers.Rules;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.Extensions.BCL;
using NexusMods.Extensions.Hashing;
using NexusMods.Hashing.xxHash64;
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

    /// <summary>
    /// Connection.
    /// </summary>
    protected readonly IConnection Connection;

    /// <summary>
    /// Loadout synchronizer base constructor.
    /// </summary>
    protected ALoadoutSynchronizer(
        ILogger logger,
        IFileStore fileStore,
        ISorter sorter,
        IConnection conn,
        IOSInformation os)
    {
        _logger = logger;
        _fileStore = fileStore;
        _sorter = sorter;
        Connection = conn;
        _os = os;
    }

    /// <summary>
    /// Helper constructor that takes only a service provider, and resolves the dependencies from it.
    /// </summary>
    /// <param name="provider"></param>
    protected ALoadoutSynchronizer(IServiceProvider provider) : this(
        provider.GetRequiredService<ILogger<ALoadoutSynchronizer>>(),
        provider.GetRequiredService<IFileStore>(),
        provider.GetRequiredService<ISorter>(),
        provider.GetRequiredService<IConnection>(),
        provider.GetRequiredService<IOSInformation>()
    )
    {
    }

    private void CleanDirectories(IEnumerable<GamePath> toDelete, DiskState newState, GameInstallation installation)
    {
        var seenDirectories = new HashSet<GamePath>();
        var directoriesToDelete = new HashSet<GamePath>();

        var newStatePaths = newState.Select(e => (GamePath)e.Path).ToHashSet();

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



    public SyncTree BuildSyncTree(DiskState currentState, DiskState previousTree, IEnumerable<LoadoutItem.ReadOnly> loadoutItems)
    {
        var grouped = loadoutItems
            .OfTypeLoadoutItemWithTargetPath()
            .Where(x => FileIsEnabled(x.AsLoadoutItem()))
            .GroupBy(f => (GamePath)f.TargetPath)
            .Select(group =>
            {
                var file = group.First();
                if (group.Count() > 1)
                {
                    file = SelectWinningFile(group);
                }
                return file;
            })
            .Where(f => !f.TryGetAsDeletedFile(out _))
            .Where(f => !IsIgnoredPath(f.TargetPath))
            .OfTypeLoadoutFile();
        
        return BuildSyncTree(currentState, previousTree, grouped);
    }

    /// <inheritdoc />
    public SyncTree BuildSyncTree<T>(DiskState currentState, DiskState previousTree, IEnumerable<T> loadoutItems)
    where T : IHavePathHashSizeAndReference
    {
        var tree = new Dictionary<GamePath, SyncTreeNode>();
        
        foreach (var item in loadoutItems)
        {
            
            tree.Add(item.Path, new SyncTreeNode
                {
                    Path = item.Path,
                    LoadoutFileHash = item.Hash,
                    LoadoutFileSize = item.Size,
                    LoadoutFileId = item.Reference,
                }
            );
        }

        foreach (var node in previousTree)
        {
            if (tree.TryGetValue(node.Path, out var found))
            {
                found.Previous = node;
            }
            else
            {
                tree.Add(node.Path, new SyncTreeNode
                    {
                        Path = node.Path,
                        Previous = node,
                    }
                );
            }
        }

        foreach (var node in currentState)
        {
            if (tree.TryGetValue(node.Path, out var found))
            {
                found.Disk = node;
            }
            else
            {
                tree.Add(node.Path, new SyncTreeNode
                    {
                        Path = node.Path,
                        Disk = node,
                    }
                );
            }
        }

        return new SyncTree(tree);
    }

    /// <summary>
    /// Returns true if the file and all its parents are not disabled.
    /// </summary>
    private static bool FileIsEnabled(LoadoutItem.ReadOnly arg)
    {
        return !arg.GetThisAndParents().Any(f => f.Contains(LoadoutItem.Disabled));
    }

    /// <inheritdoc />
    public async Task<SyncTree> BuildSyncTree(Loadout.ReadOnly loadout)
    {
        var metadata = await ReindexState(loadout.InstallationInstance, Connection);
        var previouslyApplied = loadout.Installation.GetLastAppliedDiskState();
        return BuildSyncTree(metadata.DiskStateEntries, previouslyApplied, loadout.Items);
    }

    /// <inheritdoc />
    public SyncActionGroupings<SyncTreeNode> ProcessSyncTree(SyncTree tree)
    {
        var groupings = new SyncActionGroupings<SyncTreeNode>();

        foreach (var entry in tree.GetAllDescendentFiles())
        {
            var item = entry.Item.Value;


            var signature = new SignatureBuilder
            {
                DiskHash = item.Disk.HasValue ? item.Disk.Value.Hash : Optional<Hash>.None,
                PrevHash = item.Previous.HasValue ? item.Previous.Value.Hash : Optional<Hash>.None,
                LoadoutHash = item.LoadoutFileHash.HasValue ? item.LoadoutFileHash.Value : Optional<Hash>.None,
                DiskArchived = item.Disk.HasValue && HaveArchive(item.Disk.Value.Hash),
                PrevArchived = item.Previous.HasValue && HaveArchive(item.Previous.Value.Hash),
                LoadoutArchived = item.LoadoutFileHash.HasValue && HaveArchive(item.LoadoutFileHash.Value),
                PathIsIgnored = IsIgnoredBackupPath(item.Path),
            }.Build();

            item.Signature = signature;
            item.Actions = ActionMapping.MapActions(signature);
            
            groupings.Add(item);
        }

        return groupings;
    }

    /// <inheritdoc />
    public async Task<Loadout.ReadOnly> RunGroupings(SyncTree tree, SyncActionGroupings<SyncTreeNode> groupings, Loadout.ReadOnly loadout)
    {
        using var tx = Connection.BeginTransaction();
        var gameMetadataId = loadout.InstallationInstance.GameMetadataId;
        var register = loadout.InstallationInstance.LocationsRegister;

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
                    await ActionBackupFiles(groupings, loadout.InstallationInstance);
                    break;

                case Actions.IngestFromDisk:
                    await ActionIngestFromDisk(groupings, loadout, tx);
                    break;

                case Actions.DeleteFromDisk:
                    ActionDeleteFromDisk(groupings, register, tx);
                    break;

                case Actions.ExtractToDisk:
                    await ActionExtractToDisk(groupings, register, tx,
                        gameMetadataId
                    );
                    break;

                case Actions.AddReifiedDelete:
                    ActionAddReifiedDelete(groupings, loadout, tx);
                    break;

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
    public async Task RunGroupings(SyncTree tree, SyncActionGroupings<SyncTreeNode> groupings, GameInstallation gameInstallation)
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
            tx.Retract(gameMetadataId, GameInstallMetadata.LastSyncedLoadout, gameMetadata.LastSyncedLoadout);
            tx.Retract(gameMetadataId, GameInstallMetadata.LastSyncedLoadoutTransaction, gameMetadata.LastSyncedLoadoutTransaction);
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

    private async Task ActionExtractToDisk(SyncActionGroupings<SyncTreeNode> groupings, IGameLocationsRegister register, ITransaction tx, EntityId gameMetadataId)
    {
        // Extract files to disk
        var toExtract = groupings[Actions.ExtractToDisk];
        _logger.LogDebug("Extracting {Count} files to disk", toExtract.Count);
        if (toExtract.Count > 0)
        {
            await _fileStore.ExtractFiles(toExtract.Select(item =>
                    {
                        var gamePath = register.GetResolvedPath(item.Path);
                        if (!item.LoadoutFileHash.HasValue)
                        {
                            throw new InvalidOperationException("File found in tree processing is not a loadout file, this should not happen (until generated files are implemented)");
                        }

                        return (item.LoadoutFileHash.Value, gamePath);
                    }
                ), CancellationToken.None
            );

            var isUnix = _os.IsUnix();
            foreach (var entry in toExtract)
            {
                if (!entry.LoadoutFileHash.HasValue)
                {
                    throw new InvalidOperationException("File found in tree processing is not a loadout file, this should not happen (until generated files are implemented)");
                }

                // Reuse the old disk state entry if it exists
                if (entry.Disk.HasValue)
                {
                    tx.Add(entry.Disk.Value.Id, DiskStateEntry.Hash, entry.LoadoutFileHash.Value);
                    tx.Add(entry.Disk.Value.Id, DiskStateEntry.Size, entry.LoadoutFileSize.Value);
                    tx.Add(entry.Disk.Value.Id, DiskStateEntry.LastModified, DateTime.UtcNow);
                }
                else
                {
                    _ = new DiskStateEntry.New(tx, tx.TempId(DiskStateEntry.EntryPartition))
                    {
                        Path = entry.Path.ToGamePathParentTuple(gameMetadataId),
                        Hash = entry.LoadoutFileHash.Value,
                        Size = entry.LoadoutFileSize.Value,
                        LastModified = DateTime.UtcNow,
                        GameId = gameMetadataId,
                    };
                }


                // And mark them as executable if necessary, on Unix
                if (!isUnix)
                    continue;

                var path = register.GetResolvedPath(entry.Path);
                var ext = path.Extension.ToString().ToLower();
                if (ext is not ("" or ".sh" or ".bin" or ".run" or ".py" or ".pl" or ".php" or ".rb" or ".out"
                    or ".elf")) continue;

                // Note (Sewer): I don't think we'd ever need anything other than just 'user' execute, but you can never
                // be sure. Just in case, I'll throw in group and other to match 'chmod +x' behaviour.
                var currentMode = path.GetUnixFileMode();
                path.SetUnixFileMode(currentMode | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
            }
        }
    }

    private void ActionDeleteFromDisk(SyncActionGroupings<SyncTreeNode> groupings, IGameLocationsRegister register, ITransaction tx)
    {
        // Delete files from disk
        var toDelete = groupings[Actions.DeleteFromDisk];
        _logger.LogDebug("Deleting {Count} files from disk", toDelete.Count);
        foreach (var item in toDelete)
        {
            var gamePath = register.GetResolvedPath(item.Path);
            gamePath.Delete();

            // Don't delete the entry if we're just going to replace it
            if (!item.Actions.HasFlag(Actions.ExtractToDisk))
            {
                var id = item.Disk.Value.Id;
                tx.Retract(id, DiskStateEntry.Path, item.Disk.Value.Path);
                tx.Retract(id, DiskStateEntry.Hash, item.Disk.Value.Hash);
                tx.Retract(id, DiskStateEntry.Size, item.Disk.Value.Size);
                tx.Retract(id, DiskStateEntry.LastModified, item.Disk.Value.LastModified);
                tx.Retract(id, DiskStateEntry.Game, item.Disk.Value.Game);
            }
        }
    }

    private async Task ActionBackupFiles(SyncActionGroupings<SyncTreeNode> groupings, GameInstallation gameInstallation)
    {
        var toBackup = groupings[Actions.BackupFile];
        _logger.LogDebug("Backing up {Count} files", toBackup.Count);

        await BackupNewFiles(gameInstallation, toBackup.Select(item =>
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
            tx.Add(file.Disk.Value.Id, DiskStateEntry.LastModified, DateTime.UtcNow);
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
        if (GameInstallMetadata.LastSyncedLoadout.TryGet(loadout.Installation, out var lastAppliedId) && lastAppliedId != loadout.Id)
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

        int GetPriority(LoadoutItemWithTargetPath.ReadOnly item)
        {
            foreach (var parent in item.AsLoadoutItem().GetThisAndParents())
            {
                if (!parent.TryGetAsLoadoutItemGroup(out var group))
                    continue;

                if (group.TryGetAsLoadoutGameFilesGroup(out var gameFilesGroup))
                    return 0;
            }

            return 50;
        }
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
        var syncTree = BuildSyncTree(diskState, diskState, loadout.Items);
        // Process the sync tree to get the actions populated in the nodes
        ProcessSyncTree(syncTree);

        List<DiskDiffEntry> diffs = new();

        foreach (var node in syncTree.GetAllDescendentFiles())
        {
            var syncNode = node.Item.Value;
            var actions = syncNode.Actions;
            
            if (actions.HasFlag(Actions.DoNothing))
            {
                var entry = new DiskDiffEntry
                {
                    Hash = node.Item.Value.LoadoutFileHash.Value,
                    Size = node.Item.Value.LoadoutFileSize.Value,
                    ChangeType = FileChangeType.None,
                    GamePath = node.GamePath(),
                };
                diffs.Add(entry);
            }
            else if (actions.HasFlag(Actions.ExtractToDisk))
            {
                var entry = new DiskDiffEntry
                {
                    Hash = node.Item.Value.LoadoutFileHash.Value,
                    Size = node.Item.Value.LoadoutFileSize.Value,
                    // If paired with a delete action, this is a modified file not a new one
                    ChangeType = actions.HasFlag(Actions.DeleteFromDisk) ? FileChangeType.Modified : FileChangeType.Added,
                    GamePath = node.GamePath(),
                };
                diffs.Add(entry);
            }
            else if (actions.HasFlag(Actions.DeleteFromDisk))
            {
                var entry = new DiskDiffEntry
                {
                    Hash = node.Item.Value.Disk.Value.Hash,
                    Size = node.Item.Value.Disk.Value.Size,
                    ChangeType = FileChangeType.Removed,
                    GamePath = node.GamePath(),
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
                    GamePath = node.GamePath(),
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
    public virtual async Task BackupNewFiles(GameInstallation installation, IEnumerable<(GamePath To, Hash Hash, Size Size)> files)
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
        await Parallel.ForEachAsync(files, async (file, _) =>
            {
                var path = installation.LocationsRegister.GetResolvedPath(file.To);
                if (await _fileStore.HaveFile(file.Hash))
                    return;

                var archivedFile = new ArchivedFileEntry
                {
                    Size = file.Size,
                    Hash = file.Hash,
                    StreamFactory = new NativeFileStreamFactory(path),
                };

                archivedFiles.Add(archivedFile);
            }
        );

        await _fileStore.BackupFiles(archivedFiles);
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

                            // If the files don't match, update the entry
                            if (fileInfo.LastWriteTimeUtc > entry.LastModified || fileInfo.Size != entry.Size)
                            {
                                var newHash = await file.XxHash64Async();
                                tx.Add(entry.Id, DiskStateEntry.Size, fileInfo.Size);
                                tx.Add(entry.Id, DiskStateEntry.Hash, newHash);
                                tx.Add(entry.Id, DiskStateEntry.LastModified, fileInfo.LastWriteTimeUtc);
                                changes = true;
                            }
                        }
                        else
                        {
                            // No previous entry found, so create a new one
                            var newHash = await file.XxHash64Async(token: token);
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
        
        foreach (var location in installation.LocationsRegister.GetTopLevelLocations())
        {
            if (!location.Value.DirectoryExists())
                continue;

            await Parallel.ForEachAsync(location.Value.EnumerateFiles(), async (file, token) =>
                {
                    var gamePath = installation.LocationsRegister.ToGamePath(file);
                    if (IsIgnoredPath(gamePath))
                    {
                        return;
                    }

                    var newHash = await file.XxHash64Async(token: token);
                    _ = new DiskStateEntry.New(tx, tx.TempId(DiskStateEntry.EntryPartition))
                    {
                        Path = gamePath.ToGamePathParentTuple(metaDataId),
                        Hash = newHash,
                        Size = file.FileInfo.Size,
                        LastModified = file.FileInfo.LastWriteTimeUtc,
                        GameId = metaDataId
                    };
                }
            );
        }
    }

    /// <inheritdoc />
    public virtual async Task<Loadout.ReadOnly> CreateLoadout(GameInstallation installation, string? suggestedName = null)
    {
        // Get the initial state of the game folder
        var initialState = await GetOrCreateInitialDiskState(installation);

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

        var loadout = new Loadout.New(tx)
        {
            Name = suggestedName ?? installation.Game.Name + " " + shortName,
            ShortName = shortName,
            InstallationId = installation.GameMetadataId,
            Revision = 0,
            LoadoutKind = LoadoutKind.Default,
        };

        var gameFiles = CreateLoadoutGameFilesGroup(loadout, installation, tx);

        // Backup the files
        var filesToBackup = new List<(GamePath To, Hash Hash, Size Size)>();

        foreach (var file in initialState)
        {
            GamePath path = file.Path;

            if (!IsIgnoredBackupPath(path))
                filesToBackup.Add((path, file.Hash, file.Size));

            _ = new LoadoutFile.New(tx, out var loadoutFileId)
            {
                Hash = file.Hash,
                Size = file.Size,
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(tx, loadoutFileId)
                {
                    TargetPath = path.ToGamePathParentTuple(loadout.Id),
                    LoadoutItem = new LoadoutItem.New(tx, loadoutFileId)
                    {
                        Name = path.FileName,
                        LoadoutId = loadout,
                        ParentId = gameFiles.Id,
                    },
                },
            };
        }

        await BackupNewFiles(installation, filesToBackup);

        // Commit the transaction as of this point the loadout is live
        var result = await tx.Commit();

        // Remap the ids
        var remappedLoadout = result.Remap(loadout);
        
        // If this is the only loadout, activate it
        if (isOnlyLoadout)
        {
            await ActivateLoadout(remappedLoadout.Id);
        }

        return remappedLoadout;
    }

    /// <inheritdoc />
    public async Task DeactivateCurrentLoadout(GameInstallation installation)
    {
        var metadata = installation.GetMetadata(Connection);
        
        if (!metadata.Contains(GameInstallMetadata.LastSyncedLoadout))
            return;
        
        // Synchronize the last applied loadout, so we don't lose any changes
        await Synchronize(Loadout.Load(Connection.Db, metadata.LastSyncedLoadout));
        
        await ResetToOriginalGameState(installation);
    }

    /// <inheritdoc />
    public Optional<LoadoutId> GetCurrentlyActiveLoadout(GameInstallation installation)
    {
        var metadata = installation.GetMetadata(Connection);
        if (!GameInstallMetadata.LastSyncedLoadout.TryGet(metadata, out var lastAppliedLoadout))
            return Optional<LoadoutId>.None;
        return LoadoutId.From(lastAppliedLoadout);
    }

    public async Task ActivateLoadout(LoadoutId loadoutId)
    {
        var loadout = Loadout.Load(Connection.Db, loadoutId);
        var reindexed = await ReindexState(loadout.InstallationInstance, Connection);
        
        var tree = BuildSyncTree(reindexed.DiskStateEntries, reindexed.DiskStateEntries, loadout.Items);
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
    public async Task UnManage(GameInstallation installation)
    {
        var metadata = installation.GetMetadata(Connection);
        
        if (GetCurrentlyActiveLoadout(installation).HasValue)
            await DeactivateCurrentLoadout(installation);

        foreach (var loadout in metadata.Loadouts)
        {
            _logger.LogInformation("Deleting loadout {Loadout} - {ShortName}", loadout.Name, loadout.ShortName);
            await DeleteLoadout(loadout);
        }
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
    public async Task CopyLoadout(Loadout.ReadOnly loadout)
    {
        Memory<byte> buffer = GC.AllocateUninitializedArray<byte>(1024);
        
        var baseDb = loadout.Rebase().Db;

        var registry = baseDb.Registry;
        var nameId = Loadout.Name.GetDbId(registry.Id);
        var shortNameId = Loadout.ShortName.GetDbId(registry.Id);
        
        var newShortName = LoadoutNameProvider.GetNewShortName(Loadout.All(baseDb).Select(l => l.ShortName).ToArray());
        var newName = loadout.Name + " Copy";
        
        Dictionary<EntityId, EntityId> entityIdList = new();
        var remapFn = RemapFn;
        
        using var tx = Connection.BeginTransaction();

        entityIdList[loadout.Id] = tx.TempId();
        
        foreach (var item in loadout.Items)
        {
            entityIdList[item.Id] = tx.TempId();
        }

        foreach (var (oldId, newId) in entityIdList)
        {
            var entity = baseDb.Get(oldId);
            
            foreach (var datom in entity)
            {
                if (datom.A == nameId)
                {
                    tx.Add(newId, Loadout.Name, newName);
                    continue;
                }
                else if (datom.A == shortNameId)
                {
                    tx.Add(newId, Loadout.ShortName, newShortName);
                    continue;
                }

                if (buffer.Length < datom.ValueSpan.Length)
                    buffer = GC.AllocateUninitializedArray<byte>(datom.ValueSpan.Length);
                
                datom.ValueSpan.CopyTo(buffer.Span);
                
                var prefix = new KeyPrefix(newId, datom.A, TxId.Tmp, false, datom.Prefix.ValueTag);
                var newDatom = new Datom(prefix, buffer[..datom.ValueSpan.Length], registry);
                
                var attr = registry.GetAttribute(datom.A);
                attr.Remap(remapFn, buffer[..datom.ValueSpan.Length].Span);
                tx.Add(newDatom);
            }
        }

        await tx.Commit();

        return;
        
        EntityId RemapFn(EntityId entityId)
        {
            return entityIdList.GetValueOrDefault(entityId, entityId);
        }

    }


    /// <inheritdoc />
    public async Task DeleteLoadout(LoadoutId loadoutId)
    {
        var loadout = Loadout.Load(Connection.Db, loadoutId);
        var metadata = GameInstallMetadata.Load(Connection.Db, loadout.InstallationInstance.GameMetadataId);
        if (GameInstallMetadata.LastSyncedLoadout.TryGet(metadata, out var lastAppliedLoadout) && lastAppliedLoadout == loadoutId.Value)
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
    }

    /// <summary>
    ///     Creates a 'Vanilla State Loadout', which is a loadout embued with the initial
    ///     state of the game folder.
    ///
    ///     This loadout is created when the last applied loadout for a game
    ///     is deleted. And is deleted when a non-vanillastate loadout is applied.
    ///     It should be a singleton.
    /// </summary>
    private async Task<Loadout.ReadOnly> CreateVanillaStateLoadout(GameInstallation installation)
    {
        var initialState = await GetOrCreateInitialDiskState(installation);

        using var tx = Connection.BeginTransaction();
        var loadout = new Loadout.New(tx)
        {
            Name = $"Vanilla State Loadout for {installation.Game.Name}",
            ShortName = "-",
            InstallationId = installation.GameMetadataId,
            Revision = 0,
            LoadoutKind = LoadoutKind.VanillaState,
        };

        var gameFiles = CreateLoadoutGameFilesGroup(loadout, installation, tx);

        // Backup the files
        // 1. Because we need to backup the files for every created loadout.
        // 2. Because in practice this is the first loadout created.
        var filesToBackup = new List<(GamePath To, Hash Hash, Size Size)>();
        foreach (var file in initialState)
        {
            GamePath path = file.Path;

            if (!IsIgnoredBackupPath(path))
                filesToBackup.Add((path, file.Hash, file.Size));

            _ = new LoadoutFile.New(tx, out var loadoutFileId)
            {
                Hash = file.Hash,
                Size = file.Size,
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(tx, loadoutFileId)
                {
                    TargetPath = path.ToGamePathParentTuple(loadout.Id),
                    LoadoutItem = new LoadoutItem.New(tx, loadoutFileId)
                    {
                        Name = path.FileName,
                        LoadoutId = loadout,
                        ParentId = gameFiles.Id,
                    },
                },
            };
        }

        await BackupNewFiles(installation, filesToBackup);
        var result = await tx.Commit();

        return result.Remap(loadout);
    }
    
    /// <inheritdoc />
    public async Task ResetToOriginalGameState(GameInstallation installation)
    {
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
    }
}

#endregion
