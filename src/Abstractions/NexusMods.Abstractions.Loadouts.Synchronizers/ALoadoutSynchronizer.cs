using System.Collections.Concurrent;
using DynamicData.Kernel;
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
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// Base class for loadout synchronizers, provides some common functionality. Does not have to be user,
/// but reduces a lot of boilerplate, and is highly recommended.
/// </summary>
public class ALoadoutSynchronizer : ILoadoutSynchronizer
{
    private readonly ILogger _logger;
    private readonly IFileHashCache _hashCache;
    private readonly IDiskStateRegistry _diskStateRegistry;
    private readonly ISorter _sorter;
    protected readonly IConnection Connection;
    private readonly IOSInformation _os;
    private IFileStore _fileStore;

    /// <summary>
    /// Loadout synchronizer base constructor.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="hashCache"></param>
    /// <param name="store"></param>
    /// <param name="diskStateRegistry"></param>
    /// <param name="fileStore"></param>
    /// <param name="sorter"></param>
    /// <param name="os"></param>
    protected ALoadoutSynchronizer(ILogger logger,
        IFileHashCache hashCache,
        IDiskStateRegistry diskStateRegistry,
        IFileStore fileStore,
        ISorter sorter,
        IConnection conn,
        IOSInformation os)
    {
        _logger = logger;
        _hashCache = hashCache;
        _diskStateRegistry = diskStateRegistry;
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
        provider.GetRequiredService<IFileHashCache>(),
        provider.GetRequiredService<IDiskStateRegistry>(),
        provider.GetRequiredService<IFileStore>(),
        provider.GetRequiredService<ISorter>(),
        provider.GetRequiredService<IConnection>(),
        provider.GetRequiredService<IOSInformation>())

    {

    }
    
    
    
    private void CleanDirectories(IEnumerable<GamePath> toDelete, DiskStateTree newTree, GameInstallation installation)
    {
        var seenDirectories = new HashSet<GamePath>();
        var directoriesToDelete = new HashSet<GamePath>();
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
                if (newTree.ContainsKey(parentPath))
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

    /// <inheritdoc />
    public virtual async Task<DiskStateTree> GetDiskState(GameInstallation installation)
    {
        return await _hashCache.IndexDiskState(installation);
    }
    
    #region ILoadoutSynchronizer Implementation
    
    protected LoadoutOverridesGroupId GetOrCreateOverridesGroup(ITransaction tx, Loadout.ReadOnly loadout)
    {
        if (LoadoutOverridesGroup.FindByOverridesFor(loadout.Db, loadout.Id).TryGetFirst(out var found))
            return found;

        var newOverrides = new LoadoutOverridesGroup.New(tx, out var id)
        {
            OverridesForId = loadout,
            LoadoutItemGroup = new LoadoutItemGroup.New(tx, id)
            {
                IsIsLoadoutItemGroupMarker = true,
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
    public SyncTree BuildSyncTree(DiskStateTree currentState, DiskStateTree previousTree, Loadout.ReadOnly loadoutTree)
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

        foreach (var node in previousTree.GetAllDescendentFiles())
        {
            if (tree.TryGetValue(node.GamePath(), out var found))
            {
                found.Previous = node.Item.Value;
            }
            else
            {
                tree.Add(node.GamePath(), new SyncTreeNode
                {
                    Path = node.GamePath(),
                    Previous = node.Item.Value,
                });
            }
        }
        
        foreach (var node in currentState.GetAllDescendentFiles())
        {
            if (tree.TryGetValue(node.GamePath(), out var found))
            {
                found.Disk = node.Item.Value;
            }
            else
            {
                tree.Add(node.GamePath(), new SyncTreeNode
                {
                    Path = node.GamePath(),
                    Disk = node.Item.Value,
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
        return arg.GetThisAndParents().Any(f => f.Contains(LoadoutItem.IsDisabledMarker));
    }

    /// <inheritdoc />
    public async Task<SyncTree> BuildSyncTree(Loadout.ReadOnly loadout)
    {
        var diskState = await GetDiskState(loadout.InstallationInstance);
        var prevDiskState = _diskStateRegistry.GetState(loadout.InstallationInstance)!;
        
        return BuildSyncTree(diskState, prevDiskState, loadout);
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
        
        var previousTree = _diskStateRegistry.GetState(loadout.InstallationInstance)!
            .GetAllDescendentFiles()
            .ToDictionary(d => d.Item.GamePath, d => d.Item.Value);
        
        foreach (var action in ActionsInOrder)
        {
            var items = groupings[action];
            if (items.Count == 0)
                continue;

            var register = loadout.InstallationInstance.LocationsRegister;

            
            switch (action)
            {
                case Actions.DoNothing:
                    break;

                case Actions.BackupFile:
                    await ActionBackupFiles(groupings, loadout);
                    break;

                case Actions.IngestFromDisk:
                    loadout = await ActionIngestFromDisk(groupings, loadout, previousTree);
                    break;
                
                case Actions.DeleteFromDisk:
                    ActionDeleteFromDisk(groupings, register, previousTree); 
                    break;
                
                case Actions.ExtractToDisk:
                    await ActionExtractToDisk(groupings, register, previousTree);
                    break;

                case Actions.AddReifiedDelete:
                    loadout = await ActionAddReifiedDelete(groupings, loadout, previousTree);
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
        
        var newTree = DiskStateTree.Create(previousTree);
        newTree.LoadoutId = loadout.Id;
        newTree.TxId = loadout.MostRecentTxId();

        // Clean up empty directories
        var deletedFiles = groupings[Actions.DeleteFromDisk];
        if (deletedFiles.Count > 0)
        {
            CleanDirectories(deletedFiles.Select(f => f.Path), newTree, loadout.InstallationInstance);
        }
        
        await _diskStateRegistry.SaveState(loadout.InstallationInstance, newTree);

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

    private async Task<Loadout.ReadOnly> ActionAddReifiedDelete(SyncActionGroupings<SyncTreeNode> groupings, Loadout.ReadOnly loadout, Dictionary<GamePath, DiskStateEntry> previousTree)
    {
        var toAddDelete = groupings[Actions.AddReifiedDelete];
        _logger.LogDebug("Adding {Count} reified deletes", toAddDelete.Count);
                    
        using var tx = Connection.BeginTransaction();
        var overridesGroup = GetOrCreateOverridesGroup(tx, loadout);

        foreach (var item in toAddDelete)
        {
            var delete = new DeletedFile.New(tx, out var id)
            {
                IsIsDeletedFileMarker = true,
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

            previousTree.Remove(item.Path);
        }
        
        await tx.Commit();
        return loadout.Rebase();
    }

    private async Task ActionExtractToDisk(SyncActionGroupings<SyncTreeNode> groupings, IGameLocationsRegister register, Dictionary<GamePath, DiskStateEntry> previousTree)
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
                previousTree[entry.Path] = new DiskStateEntry
                {
                    Hash = loadoutFile.Hash,
                    Size = loadoutFile.Size,
                    // TODO: this isn't needed and we can delete it eventually
                    LastModified = DateTime.UtcNow,
                };
                
                

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

    private void ActionDeleteFromDisk(SyncActionGroupings<SyncTreeNode> groupings, IGameLocationsRegister register, Dictionary<GamePath, DiskStateEntry> previousTree)
    {
        // Delete files from disk
        var toDelete = groupings[Actions.DeleteFromDisk];
        _logger.LogDebug("Deleting {Count} files from disk", toDelete.Count);
        foreach (var item in toDelete)
        {
            var gamePath = register.GetResolvedPath(item.Path);
            gamePath.Delete();
            previousTree.Remove(item.Path);
        }
    }

    private async Task ActionBackupFiles(SyncActionGroupings<SyncTreeNode> groupings, Loadout.ReadOnly loadout)
    {
        var toBackup = groupings[Actions.BackupFile];
        _logger.LogDebug("Backing up {Count} files", toBackup.Count);
                    
        await BackupNewFiles(loadout.InstallationInstance, toBackup.Select(item =>
            (item.Path, item.Disk.Value.Hash, item.Disk.Value.Size)));
    }

    private async Task<Loadout.ReadOnly> ActionIngestFromDisk(SyncActionGroupings<SyncTreeNode> groupings, Loadout.ReadOnly loadout, Dictionary<GamePath, DiskStateEntry> previousTree)
    {
        var toIngest = groupings[Actions.IngestFromDisk];
        _logger.LogDebug("Ingesting {Count} files", toIngest.Count);
        using var tx = Connection.BeginTransaction();
        var overridesMod = GetOrCreateOverridesGroup(tx, loadout);
                    
        var added = new List<LoadoutFile.New>();

        foreach (var file in toIngest)
        {
            var loadoutFile = new LoadoutFile.New(tx, out var id)
            {
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(tx, id)
                {
                    LoadoutItem = new LoadoutItem.New(tx, id)
                    {
                        ParentId = overridesMod.Value,
                        LoadoutId = loadout.Id,
                        Name = file.Path.FileName,
                    },
                    TargetPath = file.Path,
                },
                Hash = file.Disk.Value.Hash,
                Size = file.Disk.Value.Size,
            };
            
            added.Add(loadoutFile);
            previousTree[file.Path] = file.Disk.Value with { LastModified = DateTime.UtcNow };
        }
                    
        if (!overridesMod.Value.InPartition(PartitionId.Temp))
        {
            var mod = new Mod.ReadOnly(loadout.Db, overridesMod);
            mod.Revise(tx);
        }
        else
        {
            loadout.Revise(tx);
        }
                    
        var result = await tx.Commit();

        loadout = loadout.Rebase();

        if (added.Count > 0) 
            loadout = await MoveNewFilesToMods(loadout, added.Select(file => result.Remap(file)).ToArray());
        return loadout;
    }

    /// <inheritdoc />
    public async Task<Loadout.ReadOnly> Synchronize(Loadout.ReadOnly loadout)
    {
        var prevDiskState = _diskStateRegistry.GetState(loadout.InstallationInstance)!;

        // If we are swapping loadouts, then we need to synchronize the previous loadout first to ingest
        // any changes, then we can apply the new loadout.
        if (prevDiskState.LoadoutId != loadout.Id)
        {
            var prevLoadout = Loadout.Load(loadout.Db, prevDiskState.LoadoutId);
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
    /// <param name="loadout"></param>
    /// <param name="newFiles"></param>
    /// <returns></returns>
    protected virtual async Task<Loadout.ReadOnly> MoveNewFilesToMods(Loadout.ReadOnly loadout, LoadoutFile.ReadOnly[] newFiles)
    {
        return loadout;
    }

    /// <inheritdoc />
    public FileDiffTree LoadoutToDiskDiff(Loadout.ReadOnly loadout, DiskStateTree diskState)
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
                    Hash = loadoutFile.Hash,
                    Size = loadoutFile.Size,
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
    
    /// <inheritdoc />
    public virtual async Task<Loadout.ReadOnly> CreateLoadout(GameInstallation installation, string? suggestedName = null)
    {
        // Get the initial state of the game folder
        var (isCached, initialState) = await GetOrCreateInitialDiskState(installation);

        // We need to create a 'Vanilla State Loadout' for rolling back the game
        // to the original state before NMA touched it, if we don't already
        // have one.
        var installLocation = installation.LocationsRegister[LocationId.Game];
        var existingLoadouts = Loadout.All(Connection.Db)
            .Where(x => x.InstallationInstance.LocationsRegister[LocationId.Game] == installLocation)
            .ToArray();
        
        if (!existingLoadouts.Any(x => x.IsVanillaStateLoadout()))
        {
            await CreateVanillaStateLoadout(installation);
        }
        
        var shortName = LoadoutNameProvider.GetNewShortName(existingLoadouts
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

        foreach (var file in initialState.GetAllDescendentFiles())
        {
            var path = file.GamePath();
            
            if (!IsIgnoredBackupPath(path)) 
                filesToBackup.Add((path, file.Item.Value.Hash, file.Item.Value.Size));

            _ = new LoadoutFile.New(tx, out var loadoutFileId)
            {
                Hash = file.Item.Value.Hash,
                Size = file.Item.Value.Size,
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
        
        initialState.TxId = result.NewTx;
        initialState.LoadoutId = remappedLoadout.Id;

        
        // Reset the game folder to initial state if making a new loadout.
        // We must do this before saving state, as Apply does a diff against
        // the last state. Which will be a state from previous loadout.
        // Note(sewer): We can't just apply the new loadout here because we haven't run SaveState
        // and we can't guarantee we have a clean state without applying.
        if (isCached)
        {
            await Synchronize(remappedLoadout);
        }

        await _diskStateRegistry.SaveState(remappedLoadout.InstallationInstance, initialState);
        return remappedLoadout;
    }

    private LoadoutGameFilesGroup.New CreateLoadoutGameFilesGroup(LoadoutId loadout, GameInstallation installation, ITransaction tx)
    {
        return new LoadoutGameFilesGroup.New(tx, out var id)
        {
            GameMetadataId = installation.GameMetadataId,
            LoadoutItemGroup = new LoadoutItemGroup.New(tx, id)
            {
                IsIsLoadoutItemGroupMarker = true,
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
        // The 'Vanilla State Loadout' contains the original state.
        await ApplyVanillaStateLoadout(installation);

        // Cleanup all of the metadata left behind for this game.
        // All database information, including loadouts, initial game state and
        // TODO: Garbage Collect unused files.

        var installationLoadouts = Loadout.All(Connection.Db).
            Where(x => x.InstallationInstance.LocationsRegister[LocationId.Game] == installation.LocationsRegister[LocationId.Game]);
        using var tx = Connection.BeginTransaction();
        foreach (var loadout in installationLoadouts)
            tx.Delete(loadout, true);
        await tx.Commit();
        
        await _diskStateRegistry.ClearInitialState(installation);
    }
    
    /// <inheritdoc />
    public virtual bool IsIgnoredBackupPath(GamePath path)
    {
        return false;
    }

    /// <inheritdoc />
    public async Task DeleteLoadout(GameInstallation installation, LoadoutId id)
    {
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

            await ApplyVanillaStateLoadout(installation);
        }

        using var tx = Connection.BeginTransaction();
        tx.Delete(id, true);
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
        var (_, initialState) = await GetOrCreateInitialDiskState(installation);

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
        foreach (var file in initialState.GetAllDescendentFiles())
        {
            var path = file.GamePath();

            if (!IsIgnoredBackupPath(path)) 
                filesToBackup.Add((path, file.Item.Value.Hash, file.Item.Value.Size));

            _ = new LoadoutFile.New(tx, out var loadoutFileId)
            {
                Hash = file.Item.Value.Hash,
                Size = file.Item.Value.Size,
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
    
    #region Misc Helper Functions
    /// <summary>
    /// Checks if the last applied loadout is a 'vanilla state' loadout.
    /// </summary>
    /// <param name="installation">The game installation to check.</param>
    /// <returns>True if the last applied loadout is a vanilla state.</returns>
    private bool IsLastLoadoutAVanillaStateLoadout(GameInstallation installation)
    {
        if (!_diskStateRegistry.TryGetLastAppliedLoadout(installation, out var lastApplied))
            return false;

        var db = Connection.AsOf(lastApplied.Tx);
        return Loadout.Load(db, lastApplied.Id).IsVanillaStateLoadout();
    }

    /// <summary>
    /// By default, this method just returns the current state of the game folders. Most of the time
    /// this creates a sub-par user experience as users may have installed mods in the past and then
    /// these files will be marked as part of the game files when they are not. Properly implemented
    /// games should override this method and return only the files that are part of the game itself.
    ///
    /// Doing so, will cause the next "Ingest" to pull in the remaining files in a way consistent with
    /// the ingestion process of the game. Likely this will involve adding the files to a "Override" mod.
    /// </summary>
    /// <param name="installation"></param>
    /// <returns></returns>
    public virtual async ValueTask<(bool isCachedState, DiskStateTree tree)> GetOrCreateInitialDiskState(GameInstallation installation)
    {
        var initialState = _diskStateRegistry.GetInitialState(installation);
        if (initialState != null)
            return (true, initialState);

        var indexedState = await _hashCache.IndexDiskState(installation);
        await _diskStateRegistry.SaveInitialState(installation, indexedState);
        return (false, indexedState);
    }

    private async Task ApplyVanillaStateLoadout(GameInstallation installation)
    {
        var installLocation = installation.LocationsRegister[LocationId.Game];

        var vanillaStateLoadout = Loadout.All(Connection.Db)
            .FirstOrOptional(x => x.InstallationInstance.LocationsRegister[LocationId.Game] == installLocation
                                 && x.IsVanillaStateLoadout());
        
        if (!vanillaStateLoadout.HasValue)
            vanillaStateLoadout = await CreateVanillaStateLoadout(installation);
        
        await Synchronize(vanillaStateLoadout.Value);
    }
    #endregion

    #region Internal Helper Functions
    /// <summary>
    /// Overrides the <see cref="IFileStore"/> used.
    /// </summary>
    [Obsolete("Intended for Benchmark Use Only")] // produce warning in IDE
    internal void SetFileStore(IFileStore fs)
    {
        _fileStore = fs;
    }
    #endregion
}
