using System.Collections;
using System.Collections.Immutable;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ModLists.Markers;
using NexusMods.DataModel.ModLists.ModFiles;
using NexusMods.Interfaces;
using NexusMods.Paths;

namespace NexusMods.DataModel.ModLists;

public class ModListManager
{
    private readonly ILogger<ModListManager> _logger;
    private readonly IDataStore _store;
    private readonly Root<ListRegistry> _root;
    private readonly FileHashCache _fileHashCache;

    public ModListManager(ILogger<ModListManager> logger, IDataStore store, FileHashCache fileHashCache)
    {
        _logger = logger;
        _store = store;
        _root = new Root<ListRegistry>(RootType.ModLists, store);
        _fileHashCache = fileHashCache;
    }

    public IObservable<ListRegistry> Changes => _root.Changes.Select(r => r.New);
    public IEnumerable<ModList> AllModLists => _root.Value.Lists.Values;

    public async Task<ModListMarker> ManageGame(GameInstallation installation, string name = "", CancellationToken? token = null)
    {
        _logger.LogInformation("Indexing game files");
        using var _ = IDataStore.WithCurrent(_store);
        var gameFiles = new HashSet<AModFile>();

        foreach (var (type, path) in installation.Locations)
        {
            await foreach (var result in _fileHashCache.IndexFolder(path, token))
            {
                gameFiles.Add(new GameFile
                {
                    To = new GamePath(type, result.Path.RelativeTo(path)),
                    Installation = installation,
                    Hash = result.Hash,
                    Size = result.Size,
                    Store = _store
                });
            }
        }
        _logger.LogInformation("Creating Modlist {Name}", name);
        var mod = new Mod
        {
            Name = "Game Files",
            Files = new EntityHashSet<AModFile>(gameFiles.Select(g => g.Id)),
            Store = _store
        };
        
        var n = ModList.Empty(_store) with
        {
            Installation = installation,
            Name = name, 
            Mods = new EntityHashSet<Mod>(new [] {mod.Id})
        };
        _root.Alter(r => r with {Lists = r.Lists.With(n.ModListId, n)});
        
        _logger.LogInformation("Modlist {Name} {Id} created", name, n.ModListId);
        return new ModListMarker(this, n.ModListId);
    }

    public void Alter(ModListId id, Func<ModList, ModList> func)
    {
        _root.Alter(r =>
        {
            var newList = func(r.Lists[id]);
            return r with { Lists = r.Lists.With(newList.ModListId, newList) };
        });
    }

    public ModList Get(ModListId id)
    {
        return _root.Value.Lists[id];
    }
}