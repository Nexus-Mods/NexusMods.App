using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.Sorting;
using NexusMods.DataModel.Sorting.Rules;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.LoadoutSynchronizer;

/// <summary>
/// Base class for loadout synchronizers, provides some common functionality. Does not have to be user,
/// but reduces a lot of boilerplate, and is highly recommended.
/// </summary>
public class ALoadoutSynchronizer : IStandardizedLoadoutSynchronizer
{
    private readonly ILogger _logger;
    private readonly FileHashCache _hashCache;
    private readonly IDataStore _store;
    private readonly LoadoutRegistry _loadoutRegistry;
    private readonly DiskStateRegistry _diskStateRegistry;
    private readonly IArchiveManager _archiveManager;

    /// <summary>
    /// Loadout synchronizer base constructor.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="hashCache"></param>
    /// <param name="store"></param>
    /// <param name="loadoutRegistry"></param>
    /// <param name="diskStateRegistry"></param>
    /// <param name="archiveManager"></param>
    protected ALoadoutSynchronizer(ILogger logger,
        FileHashCache hashCache,
        IDataStore store,
        LoadoutRegistry loadoutRegistry,
        DiskStateRegistry diskStateRegistry,
        IArchiveManager archiveManager)
    {
        _logger = logger;
        _hashCache = hashCache;
        _store = store;
        _loadoutRegistry = loadoutRegistry;
        _diskStateRegistry = diskStateRegistry;
        _archiveManager = archiveManager;
    }

    /// <summary>
    /// Helper constructor that takes only a service provider, and resolves the dependencies from it.
    /// </summary>
    /// <param name="provider"></param>
    protected ALoadoutSynchronizer(IServiceProvider provider) : this(
        provider.GetRequiredService<ILogger<ALoadoutSynchronizer>>(),
        provider.GetRequiredService<FileHashCache>(),
        provider.GetRequiredService<IDataStore>(),
        provider.GetRequiredService<LoadoutRegistry>(),
        provider.GetRequiredService<DiskStateRegistry>(),
        provider.GetRequiredService<IArchiveManager>())

    {

    }

    #region IStandardizedLoadoutSynchronizer Implementation

    /// <inheritdoc />
    public async ValueTask<FlattenedLoadout> LoadoutToFlattenedLoadout(Loadout loadout)
    {
        var dict = new Dictionary<GamePath, ModFilePair>();

        var sorted = (await SortMods(loadout)).ToList();

        foreach (var mod in sorted)
        {
            if (!mod.Enabled)
                continue;

            foreach (var (_, file) in mod.Files)
            {
                if (file is not IToFile toFile)
                    continue;

                dict[toFile.To] = new ModFilePair { Mod = mod, File = file };
            }
        }

        return FlattenedLoadout.Create(dict);
    }

    /// <inheritdoc />
    public ValueTask<FileTree> FlattenedLoadoutToFileTree(FlattenedLoadout flattenedLoadout, Loadout loadout)
    {
        return ValueTask.FromResult(FileTree.Create(flattenedLoadout.GetAllDescendentFiles()
            .Select(f => KeyValuePair.Create(f.Path, f.Value!.File))));
    }


    /// <inheritdoc />
    public async Task<DiskState> FileTreeToDisk(FileTree fileTree, Loadout loadout, FlattenedLoadout flattenedLoadout, DiskState prevState, GameInstallation installation)
    {
        List<KeyValuePair<GamePath, HashedEntry>> toDelete = new();
        List<KeyValuePair<AbsolutePath, IGeneratedFile>> toWrite = new();
        List<KeyValuePair<AbsolutePath, FromArchive>> toExtract = new();

        Dictionary<GamePath, DiskStateEntry> resultingItems = new();

        // We'll start by scanning the game folders and comparing it to the previous state. Technically this is a
        // three way compare between the disk state, the previous state, and the new state. However if the disk state
        // diverges from the previous state, we'll abort, this effectively reduces the process to a two way compare.
        foreach (var (_, location) in installation.LocationsRegister.GetTopLevelLocations())
        {
            await foreach (var entry in _hashCache.IndexFolderAsync(location))
            {
                var gamePath = installation.LocationsRegister.ToGamePath(entry.Path);
                if (prevState.TryGetValue(gamePath, out var prevEntry))
                {
                    // If the file has been modified outside of the app since the last apply, we need to ingest it.
                    if (prevEntry.Value.Hash != entry.Hash)
                    {
                        HandleNeedIngest(entry);
                        throw new UnreachableException("HandleNeedIngest should have thrown");
                    }

                    // Does the file exist in the new tree?
                    if (!fileTree.TryGetValue(gamePath, out var newEntry))
                    {
                        // Don't update the results here as we'll delete the file in a bit
                        toDelete.Add(KeyValuePair.Create(gamePath, entry));
                        continue;
                    }

                    switch (newEntry.Value!)
                    {

                        case FromArchive fa:
                            // FromArchive files are special cased so we can batch them up and extract them all at once.
                            // Don't add toExtract to the results yet as we'll need to get the modified file times
                            // after we extract them
                            toExtract.Add(KeyValuePair.Create(entry.Path, fa));
                            continue;
                        case IGeneratedFile gf and IToFile:
                            // Hash for these files is generated on the fly, so we need to update it after we write it.
                            toWrite.Add(KeyValuePair.Create(entry.Path, gf));
                            continue;
                        default:
                            throw new UnreachableException("No way to handle this file");
                    }
                }

                // If we get here, the file is new, and not in the previous state, so we need to abort and do an ingest
                HandleNeedIngest(entry);
                throw new UnreachableException("HandleNeedIngest should have thrown");
            }
        }

        // Now we look for completely new files
        foreach (var (path, entry) in fileTree.GetAllDescendentFiles())
        {
            // If the file has already been handled above, skip it
            if (resultingItems.ContainsKey(path))
                continue;

            var absolutePath = installation.LocationsRegister.GetResolvedPath(path);

            switch (entry!)
            {
                case FromArchive fa:
                    // Don't add toExtract to the results yet as we'll need to get the modified file times
                    // after we extract them
                    toExtract.Add(KeyValuePair.Create(absolutePath, fa));
                    break;
                case IGeneratedFile gf and IToFile:
                    // Don't add to the results here as we'll write the file in a bit, and need the metadata
                    // after we write it.
                    toWrite.Add(KeyValuePair.Create(absolutePath, gf));
                    break;
                default:
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
            var hash = await entry.Value.Write(outputStream, loadout, flattenedLoadout, fileTree);
            if (hash == null)
            {
                outputStream.Position = 0;
                hash = await outputStream.HashingCopyAsync(Stream.Null, CancellationToken.None);
            }

            resultingItems[((IToFile)entry.Value).To] = new DiskStateEntry
            {
                Hash = hash!.Value,
                Size = Size.From((ulong)outputStream.Length),
                LastModified = entry.Key.FileInfo.LastWriteTimeUtc
            };
        }


        // Extract all the files that need extracting in one batch.
        await _archiveManager.ExtractFiles(toExtract
            .Select(f => (f.Value.Hash, f.Key)));

        // Update the resulting items with the new file times
        foreach (var (path, entry) in toExtract)
        {
            resultingItems[entry.To] = new DiskStateEntry
            {
                Hash = entry.Hash,
                Size = entry.Size,
                LastModified = path.FileInfo.LastWriteTimeUtc
            };
        }

        // Return the new tree
        return DiskState.Create(resultingItems);
    }

    /// <inheritdoc />
    public virtual async Task<DiskState> GetDiskState(GameInstallation installation)
    {
        return await _hashCache.IndexDiskState(installation);
    }

    /// <summary>
    /// Called when a file has changed during an apply operation, and a ingest is required.
    /// </summary>
    /// <param name="entry"></param>
    public virtual void HandleNeedIngest(HashedEntry entry)
    {
        throw new Exception("File changed during apply, need to ingest");
    }

    /// <inheritdoc />
    public async ValueTask<FileTree> DiskToFileTree(DiskState diskState, Loadout prevLoadout, FileTree prevFileTree, DiskState prevDiskState)
    {
        List<KeyValuePair<GamePath, AModFile>> results = new();
        foreach (var (path, newEntry) in diskState.GetAllDescendentFiles())
        {
            var absPath = prevLoadout.Installation.LocationsRegister.GetResolvedPath(path);
            if (prevDiskState.TryGetValue(path, out var prevEntry))
            {
                var prevFile = prevFileTree[path].Value!;
                if (prevEntry.Value.Hash == newEntry.Hash)
                {
                    // If the file hasn't changed, use it as-is
                    results.Add(KeyValuePair.Create(path, prevFile));
                    continue;
                }


                // Else, the file has changed, so we need to update it.
                var newFile = await HandleChangedFile(prevFile, prevEntry.Value, newEntry, path, absPath);
                results.Add(KeyValuePair.Create(path, newFile));
            }
            else
            {
                // Else, the file is new, so we need to add it.
                var newFile = await HandleNewFile(newEntry, path, absPath);
                results.Add(KeyValuePair.Create(path, newFile));
            }
        }

        // Deletes are handled implicitly as we only return files that exist in the new state.
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
    protected virtual ValueTask<AModFile> HandleNewFile(DiskStateEntry newEntry, GamePath gamePath, AbsolutePath absolutePath)
    {
        var newFile = new FromArchive
        {
            Id = ModFileId.New(),
            Hash = newEntry.Hash,
            Size = newEntry.Size,
            To = gamePath
        };
        newFile.EnsurePersisted(_store);
        return ValueTask.FromResult<AModFile>(newFile);
    }


    /// <summary>
    /// When a file is changed, this method will be called to convert the new data into a AModFile. The
    /// file on disk is still accessible via <paramref name="absolutePath"/>
    /// </summary>
    /// <param name="prevFile"></param>
    /// <param name="prevEntry"></param>
    /// <param name="newEntry"></param>
    /// <param name="gamePath"></param>
    /// <param name="absolutePath"></param>
    /// <returns></returns>
    protected virtual async ValueTask<AModFile> HandleChangedFile(AModFile prevFile, DiskStateEntry prevEntry, DiskStateEntry newEntry, GamePath gamePath, AbsolutePath absolutePath)
    {
        if (prevFile is IGeneratedFile gf)
        {
            await using var stream = absolutePath.Read();
            var entity = await gf.Update(newEntry, stream);
            entity.EnsurePersisted(_store);
            return entity;
        }

        var newFile = new FromArchive
        {
            Id = ModFileId.New(),
            Hash = newEntry.Hash,
            Size = newEntry.Size,
            To = gamePath
        };
        newFile.EnsurePersisted(_store);
        return newFile;
    }

    /// <inheritdoc />
    public ValueTask<FlattenedLoadout> FileTreeToFlattenedLoadout(FileTree fileTree, Loadout prevLoadout,
        FlattenedLoadout prevFlattenedLoadout)
    {
        var results = new List<KeyValuePair<GamePath, ModFilePair>>();
        var mods = prevLoadout.Mods.Values
            .Where(m => !string.IsNullOrWhiteSpace(m.ModCategory))
            .GroupBy(m => m.ModCategory)
            .ToDictionary(g => g.Key, g => g.First());

        // Helper function to get a mod for a given category, or create a new one if it doesn't exist.
        Mod ModForCategory(string name)
        {
            if (mods.TryGetValue(name, out var mod))
                return mod;
            var newMod = new Mod
            {
                ModCategory = name,
                Name = name,
                Id = ModId.New(),
                Enabled = true,
                Files = EntityDictionary<ModFileId, AModFile>.Empty(_store)
            };
            mods.Add(name, newMod);
            return newMod;
        }


        // Find all the files, and try to find a match in the previous state
        foreach (var (path, file) in fileTree.GetAllDescendentFiles())
        {
            if (prevFlattenedLoadout.TryGetValue(path, out var prevPair))
            {
                if (prevPair.Value!.File.DataStoreId.Equals(file!.DataStoreId))
                {
                    // File hasn't changed, so we can use the previous entry
                    results.Add(KeyValuePair.Create(path, prevPair.Value!));
                    continue;
                }
                else
                {
                    // Use the previous mod, but the new file
                    results.Add(KeyValuePair.Create(path, new ModFilePair
                    {
                        Mod = prevPair.Value!.Mod,
                        File = file
                    }));
                    continue;
                }
            }

            // Assign the new files to a mod
            var mod = GetModForNewFile(prevLoadout, path, file!, ModForCategory);
            results.Add(KeyValuePair.Create(path, new ModFilePair
            {
                Mod = mod,
                File = file!
            }));
        }

        return ValueTask.FromResult(FlattenedLoadout.Create(results));
    }

    /// <summary>
    /// If a file is new, this method will be called to get the mod for the new file. The modForCategory function
    /// can be called to get a mod for a given category, or create a new one if it doesn't exist.
    /// </summary>
    /// <param name="prevLoadout"></param>
    /// <param name="path"></param>
    /// <param name="file"></param>
    /// <param name="modForCategory"></param>
    /// <returns></returns>
    protected virtual Mod GetModForNewFile(Loadout prevLoadout, GamePath path, AModFile file, Func<string, Mod> modForCategory)
    {
        if (path.LocationId == LocationId.Preferences)
        {
            return modForCategory(Mod.PreferencesCategory);
        }
        else if (path.LocationId == LocationId.Saves)
        {
            return modForCategory(Mod.SavesCategory);
        }
        else
        {
            return modForCategory(Mod.OverridesCategory);
        }
    }

    /// <inheritdoc />
    public ValueTask<Loadout> FlattenedLoadoutToLoadout(FlattenedLoadout flattenedLoadout, Loadout prevLoadout, FlattenedLoadout prevFlattenedLoadout)
    {
        return ValueTask.FromResult(new FlattenedToLoadoutTransformer(flattenedLoadout, prevLoadout, prevFlattenedLoadout)
            .Transform(prevLoadout));
    }

    /// <inheritdoc />
    public virtual Loadout MergeLoadouts(Loadout loadoutA, Loadout loadoutB)
    {
        var visitor = new MergingVisitor();
        return visitor.Transform(loadoutA, loadoutB);
    }

    #endregion

    #region ILoadoutSynchronizer Implementation

    /// <summary>
    /// Applies a loadout to the game folder.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    public virtual async Task<DiskState> Apply(Loadout loadout)
    {
        var flattened = await LoadoutToFlattenedLoadout(loadout);
        var fileTree = await FlattenedLoadoutToFileTree(flattened, loadout);
        var prevState = _diskStateRegistry.GetState(loadout.LoadoutId)!;
        var diskState = await FileTreeToDisk(fileTree, loadout, flattened, prevState, loadout.Installation);
        _diskStateRegistry.SaveState(loadout.LoadoutId, diskState);
        return diskState;
    }

    /// <inheritdoc />
    public virtual async Task<Loadout> Ingest(Loadout loadout)
    {
        // Reconstruct the previous file tree
        var prevFlattenedLoadout = await LoadoutToFlattenedLoadout(loadout);
        var prevFileTree = await FlattenedLoadoutToFileTree(prevFlattenedLoadout, loadout);
        var prevDiskState = _diskStateRegistry.GetState(loadout.LoadoutId)!;

        // Get the new disk state
        var diskState = await GetDiskState(loadout.Installation);
        var fileTree = await DiskToFileTree(diskState, loadout, prevFileTree, prevDiskState);
        var flattenedLoadout = await FileTreeToFlattenedLoadout(fileTree, loadout, prevFlattenedLoadout);
        var newLoadout = await FlattenedLoadoutToLoadout(flattenedLoadout, loadout, prevFlattenedLoadout);

        await BackupNewFiles(loadout, fileTree);

        return newLoadout;
    }

    /// <summary>
    /// Backs up any new files in the loadout.
    ///
    /// </summary>
    /// <param name="loadout"></param>
    /// <param name="fileTree"></param>
    public virtual async Task BackupNewFiles(Loadout loadout, FileTree fileTree)
    {
        // During ingest, new files that haven't been seen before are fed into the game's syncronizer to convert a
        // DiskStateEntry (hash, size, path) into some sort of AModFile. By default these are converted into a "FromArchive".
        // All FromArchive does, is say that this file is copied from the downloaded archives, that is, it's not generated
        // by any extension system.
        //
        // So the problem is, the ingest process has tagged all these new files as coming from the downloads, but likely
        // they've never actually been copied/compressed into the download folders. So if we need to restore them they won't exist.
        //
        // If a game wants other types of files to be backed up, they could do so with their own logic. But backing up a
        // IGeneratedFile is pointless, since when it comes time to restore that file we'll call file.Generate on it since
        // it's a generated file.


        // Backup the files that are new or changed
        await _archiveManager.BackupFiles(await fileTree.GetAllDescendentFiles()
            .Select(n => n.Value)
            .OfType<FromArchive>()
            .SelectAsync(async f =>
            {
                var path = loadout.Installation.LocationsRegister.GetResolvedPath(f.To);
                if (await _archiveManager.HaveFile(f.Hash))
                    return null;
                return new ArchivedFileEntry
                {
                    Size = f.Size,
                    Hash = f.Hash,
                    StreamFactory = new NativeFileStreamFactory(path),
                } as ArchivedFileEntry?;
            })
            .Where(f => f != null)
            .Select(f => f!.Value)
            .ToListAsync());
    }

    /// <inheritdoc />
    public virtual async Task<Loadout> Manage(GameInstallation installation)
    {
        var initialState = await installation.Game.GetInitialDiskState(installation);

        var loadoutId = LoadoutId.Create();


        var gameFiles = new Mod()
        {
            Name = "Game Files",
            ModCategory = Mod.GameFilesCategory,
            Id = ModId.New(),
            Enabled = true,
            Files = EntityDictionary<ModFileId, AModFile>.Empty(_store).With(initialState.GetAllDescendentFiles()
                .Select(f =>
                {
                    var id = ModFileId.New();
                    return KeyValuePair.Create(id, (AModFile)new FromArchive
                    {
                        Id = id,
                        Hash = f.Value.Hash,
                        Size = f.Value.Size,
                        To = f.Path
                    });
                }))
        };

        var loadout = _loadoutRegistry.Alter(loadoutId, "Initial loadout",  loadout => loadout
            with
            {
                Name = $"Loadout {installation.Game.Name}",
                Installation = installation,
                Mods = loadout.Mods.With(gameFiles.Id, gameFiles)
            });

        _diskStateRegistry.SaveState(loadout.LoadoutId, initialState);

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
    protected virtual ValueTask<ISortRule<Mod, ModId>[]> ModSortRules(Loadout loadout, Mod mod)
    {
        return ValueTask.FromResult(mod.SortRules.ToArray());
    }


    /// <summary>
    /// Sorts the mods in a loadout.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    protected virtual async Task<IEnumerable<Mod>> SortMods(Loadout loadout)
    {
        var mods = loadout.Mods.Values.Where(mod => mod.Enabled).ToList();
        _logger.LogInformation("Sorting {ModCount} mods in loadout {LoadoutName}", mods.Count, loadout.Name);
        var modRules = await mods
            .SelectAsync(async mod => (mod.Id, await ModSortRules(loadout, mod)))
            .ToDictionaryAsync(r => r.Id, r => r.Item2);
        if (modRules.Count == 0)
            return Array.Empty<Mod>();

        var sorted = Sorter.Sort(mods, m => m.Id, m => modRules[m.Id]);
        return sorted;
    }
    #endregion
}
