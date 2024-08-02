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
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
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
    /// <summary>
    /// Connection.
    /// </summary>
    protected readonly IConnection Connection;

    private readonly ILogger _logger;
    private readonly ISorter _sorter;
    private readonly IOSInformation _os;
    private readonly IFileStore _fileStore;

    /// <summary>
    /// Loadout synchronizer base constructor.
    /// </summary>
    protected ALoadoutSynchronizer(ILogger logger,
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
        provider.GetRequiredService<IOSInformation>()) { }
    
    private void CleanDirectories(IEnumerable<GamePath> toDelete, DiskState newState, GameInstallation installation)
    {
        var seenDirectories = new HashSet<GamePath>();
        var directoriesToDelete = new HashSet<GamePath>();

        var newStatePaths = newState.Select(e => e.Path).ToHashSet();
        
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

    /// <inheritdoc />
    public SyncTree BuildSyncTree(DiskState currentState, DiskState previousTree, Loadout.ReadOnly loadoutTree)
    {
        var tree = new Dictionary<GamePath, SyncTreeNode>();

        var grouped = loadoutTree.Items
            .OfTypeLoadoutItemWithTargetPath()
            .Where(x => FileIsEnabled(x.AsLoadoutItem()))
            .GroupBy(f => f.TargetPath);
        
        foreach (var group in grouped)
        {
            var path = group.Key;
            var file = group.First();
            if (group.Count() > 1)
            {
                file = SelectWinningFile(group);
            }
            
            // Deleted file markers are not included in the sync tree
            if (file.TryGetAsDeletedFile(out _))
                continue;

            if (!file.TryGetAsLoadoutFile(out var loadoutFile))
            {
                _logger.LogWarning("File {Path} is not a stored file, skipping", path);
                continue;
            }
                
            tree.Add(path, new SyncTreeNode
            {
                Path = path,
                LoadoutFile = file,
            });
            
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
                });
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
                });
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
        var metadata = await loadout.InstallationInstance.ReindexState(Connection);
        var previouslyApplied = loadout.Installation.GetLastAppliedDiskState();
        return BuildSyncTree(metadata.DiskStateEntries, previouslyApplied, loadout);
    }

    /// <inheritdoc />
    public SyncActionGroupings<SyncTreeNode> ProcessSyncTree(SyncTree tree)
    {
        var groupings = new SyncActionGroupings<SyncTreeNode>();
        
        foreach (var entry in tree.GetAllDescendentFiles())
        {
            var item = entry.Item.Value;


            // Called out so the compiler doesn't complain about unused variables
            LoadoutFile.ReadOnly loadoutFile = default!;

            if (item.LoadoutFile.HasValue && !item.LoadoutFile.Value.TryGetAsLoadoutFile(out loadoutFile))
            {
                throw new InvalidOperationException("File found in tree processing is not a loadout file, this should not happen (until generated files are implemented)");
            }

            var signature = new SignatureBuilder
            {
                DiskHash = item.Disk.HasValue ? item.Disk.Value.Hash : Optional<Hash>.None,
                PrevHash = item.Previous.HasValue ? item.Previous.Value.Hash : Optional<Hash>.None,
                LoadoutHash = item.LoadoutFile.HasValue ? loadoutFile.Hash : Optional<Hash>.None,
                DiskArchived = item.Disk.HasValue && HaveArchive(item.Disk.Value.Hash),
                PrevArchived = item.Previous.HasValue && HaveArchive(item.Previous.Value.Hash),
                LoadoutArchived = item.LoadoutFile.HasValue && HaveArchive(loadoutFile.Hash),
                PathIsIgnored = IsIgnoredBackupPath(entry.GamePath()),
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
                    await ActionBackupFiles(groupings, loadout);
                    break;

                case Actions.IngestFromDisk:
                    await ActionIngestFromDisk(groupings, loadout, tx);
                    break;
                
                case Actions.DeleteFromDisk:
                    ActionDeleteFromDisk(groupings, register, tx); 
                    break;
                
                case Actions.ExtractToDisk:
                    await ActionExtractToDisk(groupings, register, tx, gameMetadataId);
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

        tx.Add(loadout.Id, GameMetadata.LastAppliedLoadout, EntityId.From(tx.ThisTxId.Value));
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
                    TargetPath = item.Path,
                    LoadoutItem = new LoadoutItem.New(tx, id)
                    {
                        Name = item.Path.FileName,
                        ParentId = overridesGroup.Value,
                        LoadoutId = loadout.Id,
                    },
                },
            };


            var prevId = item.Disk.Value.Id;
            tx.Add(prevId, DiskStateEntry.Path, item.Path);
            tx.Add(prevId, DiskStateEntry.Hash, Hash.Zero);
            tx.Add(prevId, DiskStateEntry.Size, Size.Zero);
            tx.Add(prevId, DiskStateEntry.LastModified, DateTime.UtcNow);
            tx.Add(prevId, DiskStateEntry.Game, item.Disk.Value.Game);
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
                if (!item.LoadoutFile.Value.TryGetAsLoadoutFile(out var loadoutFile))
                {
                    throw new InvalidOperationException("File found in tree processing is not a loadout file, this should not happen (until generated files are implemented)");
                }
                return (loadoutFile.Hash, gamePath);
            }), CancellationToken.None);

            var isUnix = _os.IsUnix();
            foreach (var entry in toExtract)
            {
                if (!entry.LoadoutFile.Value.TryGetAsLoadoutFile(out var loadoutFile))
                {
                    throw new InvalidOperationException("File found in tree processing is not a loadout file, this should not happen (until generated files are implemented)");
                }

                // Reuse the old disk state entry if it exists
                if (entry.Disk.HasValue)
                {
                    tx.Add(entry.Disk.Value.Id, DiskStateEntry.Hash, loadoutFile.Hash);
                    tx.Add(entry.Disk.Value.Id, DiskStateEntry.Size, loadoutFile.Size);
                    tx.Add(entry.Disk.Value.Id, DiskStateEntry.LastModified, DateTime.UtcNow);
                }
                else
                {
                    _ = new DiskStateEntry.New(tx, tx.TempId(DiskStateEntry.EntryPartition))
                    {
                        Path = entry.Path,
                        Hash = loadoutFile.Hash,
                        Size = loadoutFile.Size,
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
                tx.Retract(id, DiskStateEntry.Path, item.Path);
                tx.Retract(id, DiskStateEntry.Hash, item.Disk.Value.Hash);
                tx.Retract(id, DiskStateEntry.Size, item.Disk.Value.Size);
                tx.Retract(id, DiskStateEntry.LastModified, item.Disk.Value.LastModified);
                tx.Retract(id, DiskStateEntry.Game, item.Disk.Value.Game);
            }
            
        }
    }

    private async Task ActionBackupFiles(SyncActionGroupings<SyncTreeNode> groupings, Loadout.ReadOnly loadout)
    {
        var toBackup = groupings[Actions.BackupFile];
        _logger.LogDebug("Backing up {Count} files", toBackup.Count);
                    
        await BackupNewFiles(loadout.InstallationInstance, toBackup.Select(item =>
            (item.Path, item.Disk.Value.Hash, item.Disk.Value.Size)));
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
                TargetPath = file.Path,
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
    public async Task<Loadout.ReadOnly> Synchronize(Loadout.ReadOnly loadout)
    {
        // If we are swapping loadouts, then we need to synchronize the previous loadout first to ingest
        // any changes, then we can apply the new loadout.
        if (loadout.Installation.LastAppliedLoadout.Id != loadout.Id)
        {
            var prevLoadout = Loadout.Load(loadout.Db, loadout.Installation.LastAppliedLoadout.Id);
            if (prevLoadout.IsValid()) 
                await Synchronize(prevLoadout);
        }
        
        var tree = await BuildSyncTree(loadout);
        var groupings = ProcessSyncTree(tree);
        return await RunGroupings(tree, groupings, loadout);
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
        var syncTree = BuildSyncTree(diskState, diskState, loadout);
        // Process the sync tree to get the actions populated in the nodes
        ProcessSyncTree(syncTree);
        
        List<DiskDiffEntry> diffs = new();

        foreach (var node in syncTree.GetAllDescendentFiles())
        {
            var syncNode = node.Item.Value;
            var actions = syncNode.Actions;

            LoadoutFile.ReadOnly loadoutFile = default!;
            if (node.Item.Value.LoadoutFile.HasValue && !node.Item.Value.LoadoutFile.Value.TryGetAsLoadoutFile(out loadoutFile))
            {
                continue;
            }

            if (actions.HasFlag(Actions.DoNothing))
            {
                var entry = new DiskDiffEntry
                {
                    Hash = loadoutFile.Hash,
                    Size = loadoutFile.Size,
                    ChangeType = FileChangeType.None,
                    GamePath = node.GamePath(),
                };
                diffs.Add(entry);
            }
            else if (actions.HasFlag(Actions.ExtractToDisk))
            {
                var entry = new DiskDiffEntry
                {
                    Hash = loadoutFile.Hash,
                    Size = loadoutFile.Size,
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
        });

        await _fileStore.BackupFiles(archivedFiles);
    }
    
    private async Task<DiskState> GetOrCreateInitialDiskState(GameInstallation installation)
    {
        // Return any existing state
        var metadata = installation.GetMetadata(Connection);
        if (metadata.Contains(GameMetadata.InitialStateTransaction))
        {
            return metadata.DiskStateAsOf(metadata.InitialStateTransaction);
        }
        
        // Or create a new one
        using var tx = Connection.BeginTransaction();
        await installation.IndexNewState(tx);
        tx.Add(metadata.Id, GameMetadata.InitialStateTransaction, EntityId.From(tx.ThisTxId.Value));
        await tx.Commit();
        
        // Rebase the metadata to the new transaction
        metadata.Rebase();
        
        // Return the new state
        return metadata.DiskStateAsOf(metadata.InitialStateTransaction);
    }
    
    /// <inheritdoc />
    public virtual async Task<Loadout.ReadOnly> CreateLoadout(GameInstallation installation, string? suggestedName = null)
    {
        // Get the initial state of the game folder
        var initialState = await GetOrCreateInitialDiskState(installation);
        
        var shortName = LoadoutNameProvider.GetNewShortName(Loadout.All(Connection.Db)
            .Where(l => l.IsVisible())
            .Select(l=> l.ShortName)
            .ToArray()
        );

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
            var path = file.Path;
            
            if (!IsIgnoredBackupPath(path)) 
                filesToBackup.Add((path, file.Hash, file.Size));

            _ = new LoadoutFile.New(tx, out var loadoutFileId)
            {
                Hash = file.Hash,
                Size = file.Size,
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(tx, loadoutFileId)
                {
                    TargetPath = path,
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
        
        return remappedLoadout;
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
        throw new NotImplementedException();
        // The 'Vanilla State Loadout' contains the original state.
        //await ApplyVanillaStateLoadout(installation);

        // Cleanup all of the metadata left behind for this game.
        // All database information, including loadouts, initial game state and
        // TODO: Garbage Collect unused files.

        var installationLoadouts = Loadout.All(Connection.Db).
            Where(x => x.InstallationInstance.LocationsRegister[LocationId.Game] == installation.LocationsRegister[LocationId.Game]);
        using var tx = Connection.BeginTransaction();
        foreach (var loadout in installationLoadouts)
            tx.Delete(loadout, true);
        await tx.Commit();
        
        //await _diskStateRegistry.ClearInitialState(installation);
    }
    
    /// <inheritdoc />
    public virtual bool IsIgnoredBackupPath(GamePath path)
    {
        return false;
    }

    /// <inheritdoc />
    public async Task DeleteLoadout(GameInstallation installation, LoadoutId id)
    {
        throw new NotImplementedException();
        /*
        // Clear Initial State if this is the only loadout for the game.
        // We use folder location for this.
        var installLocation = installation.LocationsRegister[LocationId.Game];
        var isLastLoadout = Loadout.All(Connection.Db)
            .Count(x => x.IsVisible() &&
                x.InstallationInstance.LocationsRegister[LocationId.Game] == installLocation) <= 1;

        if (isLastLoadout)
        {
            await UnManage(installation);
            return;
        }

        var hasLastApplied = _diskStateRegistry.TryGetLastAppliedLoadout(installation, out var loadoutWithTxId);
        
        // Note(Sewer) TxId, which affects loadout revision is irrelevant here
        // because we're deleting the loadout as a whole.
        if (hasLastApplied && id == loadoutWithTxId.Id)
        {
            /*
                Note(Sewer)

                The loadout being deleted is the currently active loadout.

                As a 'default' reasonable behaviour, we will reset the game folder
                to its initial state by using the 'vanilla state' loadout to accomodate this.

                This is a good default for many cases:
                
                - Game files are not likely to be overwritten, so this will 
                  just end up materialising into a bunch of deletes. (Very Fast)
                  
                - Ensures internal consistency. i.e. 'last applied loadout' is always
                  a valid loadout.
                  
                - Provides more backend flexibility (e.g. we can 'squash' the
                  revisions at the beginning of other loadouts without consequence.)
                  
                - Meets user UI/UX expectations. The next loadout they navigate to
                  won't be somehow magically applied.

                We may make a setting to change the behaviour in the future,
                via a setting to match user preferences, but for now this is the
                default.
            */
/*
            await ApplyVanillaStateLoadout(installation);
        }

        using var tx = Connection.BeginTransaction();
        tx.Delete(id, true);
        await tx.Commit();
        */
        
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
            var path = file.Path;

            if (!IsIgnoredBackupPath(path)) 
                filesToBackup.Add((path, file.Hash, file.Size));

            _ = new LoadoutFile.New(tx, out var loadoutFileId)
            {
                Hash = file.Hash,
                Size = file.Size,
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(tx, loadoutFileId)
                {
                    TargetPath = path,
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
#endregion
    
}
