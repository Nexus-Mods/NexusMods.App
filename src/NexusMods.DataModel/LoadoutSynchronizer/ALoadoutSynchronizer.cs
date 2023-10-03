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
using NexusMods.Paths;

namespace NexusMods.DataModel.LoadoutSynchronizer;

/// <summary>
/// Base class for loadout synchronizers, provides some common functionality. Does not have to be user,
/// but reduces a lot of boilerplate, and is highly recommended.
/// </summary>
public class ALoadoutSynchronizer : ILoadoutSynchronizer
{
    private readonly ILogger _logger;
    private readonly FileHashCache _hashCache;
    private readonly IFileSystem _fileSystem;
    private readonly IDataStore _store;
    private readonly LoadoutRegistry _loadoutRegistry;
    private readonly DiskStateRegistry _diskStateRegistry;
    private readonly IArchiveManager _archiveManager;

    /// <summary>
    /// Loadout synchronizer base constructor.
    /// </summary>
    /// <param name="logger"></param>
    protected ALoadoutSynchronizer(ILogger logger, FileHashCache hashCache, IFileSystem fileSystem, IDataStore store, LoadoutRegistry loadoutRegistry,
        DiskStateRegistry diskStateRegistry,
        IArchiveManager archiveManager)
    {
        _logger = logger;
        _hashCache = hashCache;
        _fileSystem = fileSystem;
        _store = store;
        _loadoutRegistry = loadoutRegistry;
        _diskStateRegistry = diskStateRegistry;
        _archiveManager = archiveManager;
    }

    protected ALoadoutSynchronizer(IServiceProvider provider) : this(
        provider.GetRequiredService<ILogger<ALoadoutSynchronizer>>(),
        provider.GetRequiredService<FileHashCache>(),
        provider.GetRequiredService<IFileSystem>(),
        provider.GetRequiredService<IDataStore>(),
        provider.GetRequiredService<LoadoutRegistry>(),
        provider.GetRequiredService<DiskStateRegistry>(),
        provider.GetRequiredService<IArchiveManager>())

    {

    }


    #region ILoadoutSynchronizer Implementation

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
    public async Task<DiskState> FileTreeToDisk(FileTree fileTree, DiskState prevState, GameInstallation installation)
    {
        List<HashedEntry> toDelete = new();
        List<KeyValuePair<AbsolutePath, AModFile>> toWrite = new();
        List<KeyValuePair<AbsolutePath, FromArchive>> toExtract = new();

        Dictionary<GamePath, DiskStateEntry> resultingItems = new();

        foreach (var (locationId, location) in installation.LocationsRegister.GetTopLevelLocations())
        {
            await foreach (var entry in _hashCache.IndexFolderAsync(location))
            {
                var gamePath = installation.LocationsRegister.ToGamePath(entry.Path);
                if (prevState.TryGetValue(gamePath, out var prevEntry))
                {
                    if (prevEntry.Value!.Hash != entry.Hash)
                    {
                        HandleNeedIngest(entry);
                        throw new UnreachableException("HandleNeedIngest should have thrown");
                    }

                    if (!fileTree.TryGetValue(gamePath, out var newEntry))
                    {
                        // Don't update the results here as we'll delete the file in a bit
                        toDelete.Add(entry);
                        continue;
                    }
                    else
                    {
                        // FromArchive files are special cased so we can batch them up and extract them all at once.
                        if (newEntry.Value! is FromArchive fa)
                        {
                            // Don't add toExtract to the results yet as we'll need to get the modified file times
                            // after we extract them
                            toExtract.Add(KeyValuePair.Create(entry.Path, fa));
                            continue;
                        }
                        // Hash for these files is generated on the fly, so we need to update it after we write it.
                        toWrite.Add(KeyValuePair.Create(entry.Path, newEntry.Value!));
                    }
                }
                else
                {
                    HandleNeedIngest(entry);
                    throw new UnreachableException("HandleNeedIngest should have thrown");
                }
                resultingItems.Add(gamePath, DiskStateEntry.From(entry));
            }
        }

        // Now we look for completely new files
        foreach (var (path, entry) in fileTree.GetAllDescendentFiles())
        {
            if (resultingItems.ContainsKey(path))
                continue;

            var absolutePath = installation.LocationsRegister.GetResolvedPath(path);

            if (entry! is FromArchive fa)
            {
                // Don't add toExtract to the results yet as we'll need to get the modified file times
                // after we extract them
                toExtract.Add(KeyValuePair.Create(absolutePath, fa));
            }
            else
            {
                // Don't add to the results here as we'll write the file in a bit
                toWrite.Add(KeyValuePair.Create(absolutePath, entry!));
            }
        }

        foreach (var entry in toDelete)
        {
            entry.Path.Delete();
        }

        foreach (var entry in toWrite)
        {
            throw new NotImplementedException();
        }


        // Extract all the files that need extracting in one batch.
        await _archiveManager.ExtractFiles(toExtract
            .Select(f => (f.Value.Hash, f.Key)));

        foreach (var (path, entry) in toExtract)
        {
            resultingItems.Add(entry.To, new DiskStateEntry
            {
                Hash = entry.Hash,
                Size = entry.Size,
                LastModified = path.FileInfo.LastWriteTimeUtc
            });
        }

        // Return the new tree
        return DiskState.Create(resultingItems);
    }

    /// <inheritdoc />
    public async Task<DiskState> GetDiskState(GameInstallation installation)
    {
        var hashed =
            await _hashCache.IndexFoldersAsync(installation.LocationsRegister.GetTopLevelLocations().Select(f => f.Value))
                .ToListAsync();
        return DiskState.Create(hashed.Select(h => KeyValuePair.Create(installation.LocationsRegister.ToGamePath(h.Path),
            DiskStateEntry.From(h))));
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
    public FileTree DiskToFileTree(DiskState diskState, FileTree prevFileTree)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public FlattenedLoadout FileTreeToFlattenedLoadout(FileTree fileTree, FlattenedLoadout prevFlattenedLoadout)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Loadout FlattenedLoadoutToLoadout(FlattenedLoadout flattenedLoadout, Loadout prevLoadout)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Loadout MergeLoadouts(Loadout loadoutA, Loadout loadoutB)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async ValueTask<DiskState> GetInitialGameState(GameInstallation installation)
    {
        var paths = installation.LocationsRegister.GetTopLevelLocations();
        var results = await _hashCache.IndexFoldersAsync(paths.Select(p => p.Value))
            .Select(p => KeyValuePair.Create(installation.LocationsRegister.ToGamePath(p.Path),
                new DiskStateEntry()
                {
                    Hash = p.Hash,
                    Size = p.Size,
                    LastModified = p.LastModified
                }))
            .ToListAsync();
        return DiskState.Create(results);
    }

    /// <summary>
    /// Applies a loadout to the game folder.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    public async Task<DiskState> Apply(Loadout loadout)
    {
        var flattened = await LoadoutToFlattenedLoadout(loadout);
        var fileTree = await FlattenedLoadoutToFileTree(flattened, loadout);
        var prevState = _diskStateRegistry.GetState(loadout.LoadoutId)!;
        var diskState = await FileTreeToDisk(fileTree, prevState, loadout.Installation);
        return diskState;
    }

    /// <inheritdoc />
    public Task Ingest(Loadout loadout)
    {
        throw new NotImplementedException();
    }

    public async Task<Loadout> Manage(GameInstallation installation)
    {
        var initialState = await installation.Game.GetInitialDiskState(installation);

        var loadoutId = LoadoutId.Create();

        // Save the state first incase someone is watching and needs the state immediately after creation.
        _diskStateRegistry.SaveState(loadoutId, initialState);
        var loadout = _loadoutRegistry.Alter(loadoutId, "Initial loadout",  loadout => loadout
            with
            {
                Name = $"Loadout {installation.Game.Name}",
                Installation = installation,
            });


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
