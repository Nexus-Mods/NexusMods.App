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

    /// <summary>
    /// Loadout synchronizer base constructor.
    /// </summary>
    /// <param name="logger"></param>
    protected ALoadoutSynchronizer(ILogger logger, FileHashCache hashCache, IFileSystem fileSystem, IDataStore store, LoadoutRegistry loadoutRegistry)
    {
        _logger = logger;
        _hashCache = hashCache;
        _fileSystem = fileSystem;
        _store = store;
        _loadoutRegistry = loadoutRegistry;
    }

    protected ALoadoutSynchronizer(IServiceProvider provider) : this(
        provider.GetRequiredService<ILogger<ALoadoutSynchronizer>>(),
        provider.GetRequiredService<FileHashCache>(),
        provider.GetRequiredService<IFileSystem>(),
        provider.GetRequiredService<IDataStore>(),
        provider.GetRequiredService<LoadoutRegistry>())
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

    public Task<DiskState> FileTreeToDisk(FileTree fileTree)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<DiskState> FileTreeToDisk(FileTree fileTree, DiskState prevState, GameInstallation installation)
    {
        List<HashedEntry> toDelete = new();
        List<KeyValuePair<HashedEntry, AModFile>> toWrite = new();

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
                        continue;
                    }

                    if (!fileTree.TryGetValue(gamePath, out var newEntry))
                    {
                        toDelete.Add(entry);
                        continue;
                    }
                    else
                    {
                        toWrite.Add(KeyValuePair.Create(entry, newEntry.Value!));
                    }
                }
                else
                {
                    HandleNeedIngest(entry);
                }
            }
        }

        foreach (var entry in toDelete)
        {
            entry.Path.Delete();
        }

        throw new NotImplementedException();
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
    public async Task Apply(Loadout loadout)
    {
        var flattened = await LoadoutToFlattenedLoadout(loadout);
        var fileTree = await FlattenedLoadoutToFileTree(flattened, loadout);
        throw new NotImplementedException();
        //await FileTreeToDisk(fileTree);
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
