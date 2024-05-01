using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DataModel.Entities.Sorting;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Games.Loadouts.Sorting;
using NexusMods.Abstractions.Games.Trees;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Extensions.BCL;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;
using NexusMods.Paths.Trees.Traits;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// Base class for loadout synchronizers, provides some common functionality. Does not have to be user,
/// but reduces a lot of boilerplate, and is highly recommended.
/// </summary>
public class ALoadoutSynchronizer : IStandardizedLoadoutSynchronizer
{
    private readonly ILogger _logger;
    private readonly IFileHashCache _hashCache;
    private readonly IDataStore _store;
    private readonly IDiskStateRegistry _diskStateRegistry;
    private readonly ISorter _sorter;
    private readonly IConnection _conn;
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
        IDataStore store,
        IDiskStateRegistry diskStateRegistry,
        IFileStore fileStore,
        ISorter sorter,
        IConnection conn,
        IOSInformation os)
    {
        _logger = logger;
        _hashCache = hashCache;
        _store = store;
        _diskStateRegistry = diskStateRegistry;
        _fileStore = fileStore;
        _sorter = sorter;
        _conn = conn;
        _os = os;
    }

    /// <summary>
    /// Helper constructor that takes only a service provider, and resolves the dependencies from it.
    /// </summary>
    /// <param name="provider"></param>
    protected ALoadoutSynchronizer(IServiceProvider provider) : this(
        provider.GetRequiredService<ILogger<ALoadoutSynchronizer>>(),
        provider.GetRequiredService<IFileHashCache>(),
        provider.GetRequiredService<IDataStore>(),
        provider.GetRequiredService<IDiskStateRegistry>(),
        provider.GetRequiredService<IFileStore>(),
        provider.GetRequiredService<ISorter>(),
        provider.GetRequiredService<IConnection>(),
        provider.GetRequiredService<IOSInformation>())

    {

    }

    #region IStandardizedLoadoutSynchronizer Implementation

    /// <inheritdoc />
    public async ValueTask<FlattenedLoadout> LoadoutToFlattenedLoadout(Loadout.Model loadout)
    {
        var dict = new Dictionary<GamePath, File.Model>();
        var sorted = await SortMods(loadout);

        foreach (var mod in sorted)
        {
            if (!mod.Enabled)
                continue;

            foreach (var file in mod.Files)
            {
                if (!file.TryGet(File.To, out var path))
                    continue;

                dict[path] = file;
            }
        }

        return FlattenedLoadout.Create(dict);
    }

    /// <inheritdoc />
    public ValueTask<FileTree> FlattenedLoadoutToFileTree(FlattenedLoadout flattenedLoadout, Loadout.Model loadout)
    {
        return ValueTask.FromResult(FileTree.Create(flattenedLoadout.GetAllDescendentFiles()
            .Select(f => KeyValuePair.Create(f.GamePath(), f.Item.Value))));
    }


    /// <inheritdoc />
    public async Task<DiskStateTree> FileTreeToDisk(FileTree fileTree, Loadout.Model loadout, FlattenedLoadout flattenedLoadout, DiskStateTree prevState, GameInstallation installation, bool skipIngest = false)
    {
        // Return the new tree
        return await FileTreeToDiskImpl(fileTree, loadout, flattenedLoadout, prevState, installation, true);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal async Task<DiskStateTree> FileTreeToDiskImpl(FileTree fileTree, Loadout.Model loadout, FlattenedLoadout flattenedLoadout, DiskStateTree prevState, GameInstallation installation, bool fixFileMode, bool skipIngest = false)
    {
        List<KeyValuePair<GamePath, HashedEntryWithName>> toDelete = new();
        List<KeyValuePair<AbsolutePath, File.Model>> toWrite = new();
        List<KeyValuePair<AbsolutePath, StoredFile.Model>> toExtract = new();

        Dictionary<GamePath, DiskStateEntry> resultingItems = new();

        // We'll start by scanning the game folders and comparing it to the previous state. Technically this is a
        // three way compare between the disk state, the previous state, and the new state. However if the disk state
        // diverges from the previous state, we'll abort, this effectively reduces the process to a two way compare.
        foreach (var (_, location) in installation.LocationsRegister.GetTopLevelLocations())
        {
            await foreach (var entry in _hashCache.IndexFolderAsync(location))
            {
                var gamePath = installation.LocationsRegister.ToGamePath(entry.Path);

                if (!prevState.TryGetValue(gamePath, out var prevEntry))
                {
                    // File is new, and not in the previous state, so we need to abort and do an ingest
                    if (skipIngest)
                        continue;
                        
                    HandleNeedIngest(entry);
                    throw new UnreachableException("HandleNeedIngest should have thrown");
                }
                
                if (prevEntry.Item.Value.Hash != entry.Hash)
                {
                    // File has changed, so we need to abort and do an ingest
                    if (skipIngest)
                        continue;
                    
                    HandleNeedIngest(entry);
                    throw new UnreachableException("HandleNeedIngest should have thrown");
                }

                if (!fileTree.TryGetValue(gamePath, out var newEntry))
                {
                    // File is unchanged, but is not present in the new tree, so it needs to be deleted
                    // We don't remove it from the results yet, will do during batch delete
                    toDelete.Add(KeyValuePair.Create(gamePath, entry));
                    continue;
                }
                
                // File didn't change on disk and is present in new tree
                resultingItems.Add(newEntry.GamePath(), prevEntry.Item.Value);
                
                var file = newEntry.Item.Value!;
                if (IsDeletedFile(file))
                {
                    // File is deleted in the new tree, so nothing to do
                    continue;
                }
                else if (file.Contains(StoredFile.Hash))
                {
                    // StoredFile files are special cased so we can batch them up and extract them all at once.
                    // Don't add toExtract to the results yet as we'll need to get the modified file times
                    // after we extract them
                    if (file.Get(StoredFile.Hash) == entry.Hash)
                        continue;

                    toExtract.Add(KeyValuePair.Create(entry.Path, file.Remap<StoredFile.Model>()));
                }
                else if (IsGeneratedFile(file))
                {
                    toWrite.Add(KeyValuePair.Create(entry.Path, (File.Model)file));
                }
                else
                {
                    _logger.LogError("Unknown file type: {Entity}", file);
                }
            }
        }

        // Now we look for completely new files or files that were deleted on disk
        foreach (var item in fileTree.GetAllDescendentFiles())
        {
            var path = item.GamePath();


            // If the file has already been handled above, skip it
            if (resultingItems.ContainsKey(path))
                continue;

            var absolutePath = installation.LocationsRegister.GetResolvedPath(path);
            
            if (prevState.TryGetValue(path, out var prevEntry))
            {
                // File is in new tree, was in prev disk state, but wasn't found on disk
                if (skipIngest)
                    continue;
                
                HandleNeedIngest(prevEntry.Item.Value.ToHashedEntry(absolutePath));
                throw new UnreachableException("HandleNeedIngest should have thrown");
            }


            var file = item.Item.Value!;
            if (IsDeletedFile(file))
            {
                // File is deleted in the new tree, so nothing to do
                continue;
            }
            else if (file.Contains(StoredFile.Hash))
            {
                toExtract.Add(KeyValuePair.Create(absolutePath, file.Remap<StoredFile.Model>()));
            }
            else if (IsGeneratedFile(file))
            {
                toWrite.Add(KeyValuePair.Create(absolutePath, (File.Model)file));
            }
            else
            {
                throw new UnreachableException("No way to handle this file");
            }
        }
        
        // Now delete all the files that need deleting in one batch.
        foreach (var entry in toDelete)
        {
            entry.Value.Path.Delete();
            resultingItems.Remove(entry.Key);
        }

        // Write the generated files (could be done in parallel)
        foreach (var entry in toWrite)
        {
            entry.Key.Parent.CreateDirectory();
            await using var outputStream = entry.Key.Create();
            var hash = await WriteGeneratedFile(entry.Value, outputStream, loadout, flattenedLoadout, fileTree);
            if (hash == null)
            {
                outputStream.Position = 0;
                hash = await outputStream.HashingCopyAsync(Stream.Null, CancellationToken.None);
            }

            var gamePath = loadout.Installation.LocationsRegister.ToGamePath(entry.Key);
            resultingItems[gamePath] = new DiskStateEntry
            {
                Hash = hash!.Value,
                Size = Size.From((ulong)outputStream.Length),
                LastModified = entry.Key.FileInfo.LastWriteTimeUtc
            };
        }

        // Extract all the files that need extracting in one batch.
        _logger.LogInformation("Extracting {Count} files", toExtract.Count);
        await _fileStore.ExtractFiles(GetFilesToExtract(toExtract));

        // Update the resulting items with the new file times
        var isUnix = _os.IsUnix();
        foreach (var (path, entry) in toExtract)
        {
            resultingItems[entry.To] = new DiskStateEntry
            {
                Hash = entry.Hash,
                Size = entry.Size,
                LastModified = path.FileInfo.LastWriteTimeUtc
            };

            // And mark them as executable if necessary, on Unix
            if (!isUnix || !fixFileMode)
                continue;

            var ext = path.Extension.ToString();
            if (ext is not ("" or ".sh" or ".bin" or ".run" or ".py" or ".pl" or ".php" or ".rb" or ".out"
                or ".elf")) continue;

            // Note (Sewer): I don't think we'd ever need anything other than just 'user' execute, but you can never
            // be sure. Just in case, I'll throw in group and other to match 'chmod +x' behaviour.
            var currentMode = path.GetUnixFileMode();
            path.SetUnixFileMode(currentMode | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
        }
        
        var newTree = DiskStateTree.Create(resultingItems);
        
        // We need to delete any empty directory structures that were left behind
        var seenDirectories = new HashSet<GamePath>();
        var directoriesToDelete = new HashSet<GamePath>();
        foreach (var entry in toDelete)
        {
            var parentPath = entry.Key.Parent;
            GamePath? emptyStructureRoot = null;
            while (parentPath != entry.Key.GetRootComponent)
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

        return newTree;

        // Return the new tree
        // Quick convert function such that to not be LINQ bottlenecked.
        // Needed as separate method because parent method is async.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static (Hash Hash, AbsolutePath Dest)[] GetFilesToExtract(List<KeyValuePair<AbsolutePath, StoredFile.Model>> toExtract) 
        {
            (Hash Hash, AbsolutePath Dest)[] entries = GC.AllocateUninitializedArray<(Hash Src, AbsolutePath Dest)>(toExtract.Count);
            var toExtractSpan = CollectionsMarshal.AsSpan(toExtract);
            for (var x = 0; x < toExtract.Count; x++)
            {
                ref var item = ref toExtractSpan[x];
                entries[x] = (item.Value.Hash, item.Key);
            }

            return entries;
        }
    }

    /// <inheritdoc />
    public virtual async Task<DiskStateTree> GetDiskState(GameInstallation installation)
    {
        return await _hashCache.IndexDiskState(installation);
    }

    /// <summary>
    /// Called when a file has changed during an apply operation, and a ingest is required.
    /// </summary>
    /// <param name="entry"></param>
    public virtual void HandleNeedIngest(HashedEntryWithName entry)
    {
        throw new NeedsIngestException();
    }

    /// <inheritdoc />
    public async ValueTask<FileTree> DiskToFileTree(DiskStateTree diskState, Loadout.Model prevLoadout, FileTree prevFileTree, DiskStateTree prevDiskState)
    {
        List<KeyValuePair<GamePath, File.Model>> results = new();
        var newFiles = new List<TempEntity>();
        foreach (var item in diskState.GetAllDescendentFiles())
        {
            var gamePath = item.GamePath();
            var absPath = prevLoadout.Installation.LocationsRegister.GetResolvedPath(item.GamePath());
            if (prevDiskState.TryGetValue(gamePath, out var prevEntry))
            {
                var prevFile = prevFileTree[gamePath].Item.Value!;
                if (prevEntry.Item.Value.Hash == item.Item.Value.Hash)
                {
                    // If the file hasn't changed, use it as-is
                    results.Add(KeyValuePair.Create(gamePath, prevFile));
                    continue;
                }

                // Else, the file has changed, so we need to update it.
                var newFile = await HandleChangedFile(prevFile, prevEntry.Item.Value, item.Item.Value, gamePath, absPath);
                newFiles.Add(newFile);
            }
            else
            {
                // Else, the file is new, so we need to add it.
                var newFile = await HandleNewFile(item.Item.Value, gamePath, absPath);
                newFiles.Add(newFile);
            }
        }

        if (newFiles.Count > 0)
        {
            _logger.LogInformation("Found {Count} new files during ingest", newFiles.Count);

            var overridesMod = prevLoadout.Mods.FirstOrDefault(m => m.Category == ModCategory.Overrides);
            using var tx = _conn.BeginTransaction();
            
            if (overridesMod == null)
            {
                overridesMod = new Mod.Model(tx)
                {
                    Loadout = prevLoadout,
                    Category = ModCategory.Overrides,
                    Name = "Overrides",
                    Enabled = true,
                };
            }
            
            List<EntityId> addedFiles = new();
            foreach (var newFile in newFiles)
            {
                newFile.Add(File.Loadout, prevLoadout.Id);
                newFile.Add(File.Mod, overridesMod.Id);
                newFile.AddTo(tx);
                addedFiles.Add(newFile.Id!.Value);
            }
            
            var result = await tx.Commit();

            foreach (var addedFile in addedFiles)
            {
                var storedFile = result.Db.Get<StoredFile.Model>(result[addedFile]);
                results.Add(KeyValuePair.Create(storedFile.To, (File.Model)storedFile));
            }
        }

        return FileTree.Create(results);
    }

    /// <summary>
    /// When a file is new, this method will be called to convert the new data into a AModFile. The file contents
    /// are still accessible via <paramref name="absolutePath"/>
    /// </summary>
    /// <param name="newEntry"></param>
    /// <param name="gamePath"></param>
    /// <param name="absolutePath"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    /// <returns>An unpersisted new file. This file needs to be persisted.</returns>
    protected virtual ValueTask<TempEntity> HandleNewFile(DiskStateEntry newEntry, GamePath gamePath, AbsolutePath absolutePath)
    {
        var newFile = new TempEntity
        {
            {StoredFile.Hash, newEntry.Hash},
            {StoredFile.Size, newEntry.Size},
            {File.To, gamePath},
        };
        return ValueTask.FromResult(newFile);
    }


    /// <summary>
    /// When a file is changed, this method will be called to convert the new data into a AModFile. The
    /// file on disk is still accessible via <paramref name="absolutePath"/>
    /// </summary>
    protected virtual async ValueTask<TempEntity> HandleChangedFile(File.Model prevFile, DiskStateEntry prevEntry, DiskStateEntry newEntry, GamePath gamePath, AbsolutePath absolutePath)
    {
        var newFile = new TempEntity
        {
            {StoredFile.Hash, newEntry.Hash},
            {StoredFile.Size, newEntry.Size},
            {File.To, gamePath},
        };
        return newFile;
    }

    /// <inheritdoc />
    public async ValueTask<FlattenedLoadout> FileTreeToFlattenedLoadout(FileTree fileTree, Loadout.Model prevLoadout,
        FlattenedLoadout prevFlattenedLoadout)
    {
        var resultIds = new List<(GamePath Path, EntityId Id)>();
        var results = new List<KeyValuePair<GamePath, File.Model>>();
        var mods = prevLoadout.Mods
            .GroupBy(m => m.Category)
            .ToDictionary(g => g.Key, g => g.First());
        
        using var tx = _conn.BeginTransaction();

        // Helper function to get a mod for a given category, or create a new one if it doesn't exist.
        Mod.Model ModForCategory(ModCategory category)
        {
            if (mods.TryGetValue(category, out var mod))
                return mod;
            var newMod = new Mod.Model(tx)
            {
                Category = category,
                Name = category.ToString(),
                Enabled = true,
            };
            mods.Add(category, newMod);
            return newMod;
        }

        
        // Find all the files, and try to find a match in the previous state
        foreach (var item in fileTree.GetAllDescendentFiles())
        {
            resultIds.Add((item.Item.GamePath, item.Item.Value.Id));
            // No mod, so we need to put it somewhere, for now we put it in the Override Mod
            if (!item.Item.Value.Contains(File.Mod))
            {
                var mod = ModForCategory(ModCategory.Overrides);
                tx.Add(item.Item.Value.Id, File.Mod, mod.Id);
            }
        }
        var result = await tx.Commit();

        var tree = resultIds.Select(kv => 
            KeyValuePair.Create(kv.Path, result.Db.Get<File.Model>(result[kv.Id])));
        return FlattenedLoadout.Create(tree);
    }

    #endregion

    #region ILoadoutSynchronizer Implementation

    /// <summary>
    /// Applies a loadout to the game folder.
    /// </summary>
    /// <param name="loadout"></param>
    /// <param name="forceSkipIngest">
    ///     Skips checking if an ingest is needed.
    ///     Force overrides current locations to intended tree
    /// </param>
    /// <returns></returns>
    public virtual async Task<DiskStateTree> Apply(Loadout.Model loadout, bool forceSkipIngest = false)
    {
        var flattened = await LoadoutToFlattenedLoadout(loadout);
        var fileTree = await FlattenedLoadoutToFileTree(flattened, loadout);
        var prevState = _diskStateRegistry.GetState(loadout.Installation)!;
        var diskState = await FileTreeToDisk(fileTree, loadout, flattened, prevState, loadout.Installation, forceSkipIngest);
        diskState.LoadoutId = loadout.Id;
        diskState.TxId = loadout.Db.BasisTxId;
        await _diskStateRegistry.SaveState(loadout.Installation, diskState);
        return diskState;
    }

    /// <inheritdoc />
    public virtual async Task<Loadout.Model> Ingest(Loadout.Model loadout)
    {
        // Reconstruct the previous file tree
        var prevFlattenedLoadout = await LoadoutToFlattenedLoadout(loadout);
        var prevFileTree = await FlattenedLoadoutToFileTree(prevFlattenedLoadout, loadout);
        var prevDiskState = _diskStateRegistry.GetState(loadout.Installation)!;

        // Get the new disk state
        var diskState = await GetDiskState(loadout.Installation);
        var newFiles = FindChangedFiles(diskState, loadout, prevFileTree, prevDiskState);

        await BackupNewFiles(loadout.Installation, newFiles.Select(f => 
            (f.GetFirst(File.To), 
             f.GetFirst(StoredFile.Hash), 
             f.GetFirst(StoredFile.Size))));
        var newLoadout = await AddChangedFilesToLoadout(loadout, newFiles);
        
        diskState.LoadoutId = newLoadout.Id;
        diskState.TxId = newLoadout.Db.BasisTxId;
        await _diskStateRegistry.SaveState(loadout.Installation, diskState);
        return newLoadout;
    }


    private async Task<Loadout.Model> AddChangedFilesToLoadout(Loadout.Model loadout, TempEntity[] newFiles)
    {
        var overridesMod = loadout.Mods.FirstOrDefault(m => m.Category == ModCategory.Overrides);
        
        using var tx = _conn.BeginTransaction();
        
        overridesMod ??= new Mod.Model(tx)
        {
            Loadout = loadout,
            Category = ModCategory.Overrides,
            Name = "Overrides",
            Enabled = true,
        };
        
        foreach (var newFile in newFiles)
        {
            newFile.Add(File.Loadout, loadout.Id);
            newFile.Add(File.Mod, overridesMod.Id);
            newFile.AddTo(tx);
        }
        
        var result = await tx.Commit();
        return result.Db.Get<Loadout.Model>(loadout.Id);
    }

    private TempEntity[] FindChangedFiles(DiskStateTree diskState, Loadout.Model loadout, FileTree prevFileTree, DiskStateTree prevDiskState)
    {
        List<TempEntity> newFiles = new();
        // Find files on disk that have changed or are not in the loadout
        foreach (var file in diskState.GetAllDescendentFiles())
        {
            var gamePath = file.GamePath();
            TempEntity? newFile;
            if (prevDiskState.TryGetValue(gamePath, out var prevEntry))
            {
                var prevFile = prevFileTree[gamePath].Item.Value!;
                if (prevEntry.Item.Value.Hash == file.Item.Value.Hash)
                {
                    // If the file hasn't changed, do nothing
                    continue;
                }

                if (prevFile.Mod.Category == ModCategory.Overrides)
                {
                    // Previous file was in the overrides, so we need to update it
                    newFile = new TempEntity
                    {
                        { StoredFile.Hash, file.Item.Value.Hash },
                        { StoredFile.Size, file.Item.Value.Size },
                    };
                    newFile.Id = prevFile.Id;
                }
                else
                {
                    // Previous file was not in the overrides, so we need to add it
                    newFile = new TempEntity
                    {
                        { StoredFile.Hash, file.Item.Value.Hash },
                        { StoredFile.Size, file.Item.Value.Size },
                        { File.To, gamePath },
                    };
                }
            }
            else
            {
                // Else, the file is new, so we need to add it.
                newFile = new TempEntity
                {
                    { StoredFile.Hash, file.Item.Value.Hash },
                    { StoredFile.Size, file.Item.Value.Size },
                    { File.To, gamePath },
                };
            }

            newFiles.Add(newFile);
        }
        
        // Find files in the loadout that are not on disk
        foreach (var loadoutFile in prevDiskState.GetAllDescendentFiles())
        {
            var gamePath = loadoutFile.GamePath();
            if (diskState.ContainsKey(gamePath))
                continue;
            
            var prevDbFile = prevFileTree[gamePath].Item.Value!;
            if (prevDbFile.Mod.Category == ModCategory.Overrides)
            {
                if (prevDbFile.TryGet(DeletedFile.Deleted, out var deleted) && deleted)
                {
                    // already deleted, do nothing
                    continue;
                }
                else
                {
                    var newFile = new TempEntity
                    {
                        { StoredFile.Hash, loadoutFile.Item.Value.Hash },
                        { StoredFile.Size, loadoutFile.Item.Value.Size },
                        { DeletedFile.Deleted, true },
                        { File.To, gamePath },
                    };
                    newFile.Id = prevDbFile.Id;
                    newFiles.Add(newFile);
                }
            }
            else
            {
                // New Deleted file
                var newFile = new TempEntity
                {
                    { StoredFile.Hash, loadoutFile.Item.Value.Hash },
                    { StoredFile.Size, loadoutFile.Item.Value.Size },
                    { DeletedFile.Deleted, true },
                    { File.To, gamePath },
                };
                newFiles.Add(newFile);
            }
        }
        

        return newFiles.ToArray();
    }

    /// <inheritdoc />
    public async ValueTask<FileDiffTree> LoadoutToDiskDiff(Loadout.Model loadout, DiskStateTree diskState)
    {
        var flattenedLoadout = await LoadoutToFlattenedLoadout(loadout);
        return await FlattenedLoadoutToDiskDiff(flattenedLoadout, diskState);
    }

    private ValueTask<FileDiffTree> FlattenedLoadoutToDiskDiff(FlattenedLoadout flattenedLoadout, DiskStateTree diskState)
    {
        var loadoutFiles = flattenedLoadout.GetAllDescendentFiles().ToArray();
        var diskStateEntries = diskState.GetAllDescendentFiles().ToArray();

        // With both deletions and additions it might be more than Max, but it's a starting point
        Dictionary<GamePath, DiskDiffEntry> resultingItems = new(Math.Max(loadoutFiles.Length, diskStateEntries.Length));
        
        // Add all the disk state entries to the result, checking for changes
        foreach (var diskItem in diskStateEntries)
        {
            var gamePath = diskItem.GamePath();
            if (flattenedLoadout.TryGetValue(gamePath, out var loadoutFileEntry))
            {
                var file = loadoutFileEntry.Item.Value;
                if (IsStoredFile(file))
                {
                    var sf = file.Remap<StoredFile.Model>();
                    if (sf.Hash != diskItem.Item.Value.Hash)
                    {
                        resultingItems.Add(gamePath,
                            new DiskDiffEntry
                            {
                                GamePath = gamePath,
                                Hash = sf.Hash,
                                Size = sf.Size,
                                ChangeType = FileChangeType.Modified,
                            }
                        );
                    }
                    else
                    {
                        resultingItems.Add(gamePath,
                            new DiskDiffEntry
                            {
                                GamePath = gamePath,
                                Hash = sf.Hash,
                                Size = sf.Size,
                                ChangeType = FileChangeType.None,
                            }
                        );
                    }
                }
                else if (IsGeneratedFile(file))
                {
                    // TODO: Implement change detection for generated files
                    continue;
                }
                else
                {
                    throw new NotImplementedException("No way to handle this file");
                }
            }
            else
            {
                resultingItems.Add(gamePath,
                    new DiskDiffEntry
                    {
                        GamePath = gamePath,
                        Hash = diskItem.Item.Value.Hash,
                        Size = diskItem.Item.Value.Size,
                        ChangeType = FileChangeType.Removed,
                    }
                );
            }
        }

        // Add all the new files to the result
        foreach (var loadoutFile in loadoutFiles)
        {
            var gamePath = loadoutFile.GamePath();
            var file = loadoutFile.Item.Value;
            if (IsStoredFile(file))
            {
                var sf = file.Remap<StoredFile.Model>();
                if (!resultingItems.TryGetValue(gamePath, out _))
                {
                    resultingItems.Add(gamePath,
                        new DiskDiffEntry
                        {
                            GamePath = gamePath,
                            Hash = sf.Hash,
                            Size = sf.Size,
                            ChangeType = FileChangeType.Added,
                        }
                    );
                }
            }
            else if (IsGeneratedFile(file))
            {
                // TODO: Implement change detection for generated files
                continue;
            }
            else
            {
                throw new NotImplementedException("No way to handle this file");
            }
        }

        return ValueTask.FromResult(FileDiffTree.Create(resultingItems));
    }

    /// <summary>
    /// Backs up any new files in the loadout.
    ///
    /// </summary>
    public virtual async Task BackupNewFiles(GameInstallation installation, IEnumerable<(GamePath To, Hash Hash, Size Size)> files)
    {
// During ingest, new files that haven't been seen before are fed into the game's syncronizer to convert a
        // DiskStateEntry (hash, size, path) into some sort of AModFile. By default these are converted into a "StoredFile".
        // All StoredFile does, is say that this file is copied from the downloaded archives, that is, it's not generated
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
        await Parallel.ForEachAsync(files, async (file, cancellationToken) =>
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
    public bool IsGeneratedFile(File.Model file)
    {
        return false;
    }


    /// <summary>
    /// True if the file is marked as deleted.
    /// </summary>
    public bool IsDeletedFile(File.Model file)
    {
        return file.TryGet(DeletedFile.Deleted, out var deleted) && deleted;
    }

    /// <summary>
    /// True if the file is a stored file.
    /// </summary>
    public bool IsStoredFile(File.Model file)
    {
        return file.Contains(StoredFile.Hash);
    }

    /// <inheritdoc />
    public Task<Hash?> WriteGeneratedFile(File.Model file, Stream outputStream, Loadout.Model loadout, FlattenedLoadout flattenedLoadout, FileTree fileTree)
    {
        throw new NotSupportedException("This method should be overridden by the game's syncronizer.");
    }

    /// <inheritdoc />
    public virtual async Task<Loadout.Model> Manage(GameInstallation installation, string? suggestedName = null)
    {
        // Get the initial state of the game folders
        var (isCached, initialState) = await GetOrCreateInitialDiskState(installation);
        
        using var tx = _conn.BeginTransaction();

        // Add the loadout
        var loadout = new Loadout.Model(tx)
        {
            Name = suggestedName ?? $"Loadout {installation.Game.Name}",
            Installation = installation,
        };
        
        var gameFiles = new Mod.Model(tx)
        {
            Name = "Game Files",
            Category = ModCategory.GameFiles,
            Enabled = true,
            Loadout = loadout,
        };

        var filesToBackup = new List<(GamePath To, Hash Hash, Size Size)>();
        foreach (var file in initialState.GetAllDescendentFiles())
        {
            var path = file.GamePath();
            filesToBackup.Add((path, file.Item.Value.Hash, file.Item.Value.Size));
            _ = new StoredFile.Model(tx)
            {
                Loadout = loadout,
                Mod = gameFiles,
                Hash = file.Item.Value.Hash,
                Size = file.Item.Value.Size,
                To = path,
            };
        }
        
        // Backup the files
        await BackupNewFiles(installation, filesToBackup);
        
        // Commit the transaction as of this point the loadout is live
        var result = await tx.Commit();
        
        // Remap the ids
        loadout = result.Remap(loadout);
        gameFiles = result.Remap(gameFiles);
        
        initialState.TxId = result.NewTx;
        initialState.LoadoutId = loadout.Id;

        // Reset the game folder to initial state if making a new loadout.
        // We must do this before saving state, as Apply does a diff against
        // the last state. Which will be a state from previous loadout.
        if (isCached)
        {
            // This is a 'fast apply' operation, that avoids recomputing the file tree.
            // And avoids a double save-state.
            var flattened = await LoadoutToFlattenedLoadout(loadout);
            var prevState = _diskStateRegistry.GetState(loadout.Installation)!;
            
            var fileTree = FileTree.Create(gameFiles.Files.Select(file =>
                    {
                        var storedFile = file.Remap<StoredFile.Model>();
                        return KeyValuePair.Create(storedFile.To, file);
                    }
                )
            );
            
            await FileTreeToDisk(fileTree, loadout, flattened, prevState, loadout.Installation, true);

            // Note: DiskState returned from `FileTreeToDisk` and `initialState`
            // are the same in terms of content!!
        }

        await _diskStateRegistry.SaveState(loadout.Installation, initialState);
        return loadout;
    }

    #endregion

    #region FlattenLoadoutToTree Methods

    /// <summary>
    /// Returns the sort rules for a given mod, the loadout is given for context.
    /// </summary>
    /// <param name="loadout"></param>
    /// <param name="mod"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    protected virtual async ValueTask<ISortRule<Mod.Model, ModId>[]> ModSortRules(Loadout.Model loadout, Mod.Model mod)
    {
        if (mod.Category == ModCategory.GameFiles)
            return [new First<Mod.Model, ModId>()];
        if (mod.TryGet(Mod.SortAfter, out var other))
            return [new After<Mod.Model, ModId> { Other = ModId.From(other) }];
        return [];
    }


    /// <summary>
    /// Sorts the mods in a loadout.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    protected virtual async Task<IEnumerable<Mod.Model>> SortMods(Loadout.Model loadout)
    {
        var mods = loadout.Mods.Where(mod => mod.Enabled).ToList();
        _logger.LogInformation("Sorting {ModCount} mods in loadout {LoadoutName}", mods.Count, loadout.Name);
        var modRules = await mods
            .SelectAsync(async mod => (mod.Id, await ModSortRules(loadout, mod)))
            .ToDictionaryAsync(r => r.Id, r => r.Item2);
        if (modRules.Count == 0)
            return Array.Empty<Mod.Model>();

        var sorted = _sorter.Sort(mods, m => ModId.From(m.Id), m => modRules[m.Id]);
        return sorted;
    }
    #endregion


    #region Misc Helper Functions

    /// <summary>
    /// By default this method just returns the current state of the game folders. Most of the time
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
