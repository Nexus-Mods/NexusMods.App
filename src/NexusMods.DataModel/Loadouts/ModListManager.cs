using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Interfaces;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts;

public class LoadoutManager
{
    private readonly ILogger<LoadoutManager> _logger;
    private readonly IDataStore _store;
    private readonly Root<ListRegistry> _root;
    public readonly FileHashCache FileHashCache;
    public readonly ArchiveManager ArchiveManager;
    private readonly IModInstaller[] _installers;
    private readonly ArchiveContentsCache _analyzer;

    public LoadoutManager(ILogger<LoadoutManager> logger,
        IResource<LoadoutManager, Size> limiter,
        ArchiveManager archiveManager, 
        IDataStore store, FileHashCache fileHashCache, IEnumerable<IModInstaller> installers, ArchiveContentsCache analyzer)
    {
        _logger = logger;
        Limiter = limiter;
        ArchiveManager = archiveManager;
        _store = store;
        _root = new Root<ListRegistry>(RootType.Loadouts, store);
        FileHashCache = fileHashCache;
        _installers = installers.ToArray();
        _analyzer = analyzer;
    }

    public IResource<LoadoutManager,Size> Limiter { get; set; }

    public IObservable<ListRegistry> Changes => _root.Changes.Select(r => r.New);
    public IEnumerable<LoadoutMarker> AllLoadouts => _root.Value.Lists.Values.Select(m => new LoadoutMarker(this, m.LoadoutId));

    public async Task<LoadoutMarker> ManageGame(GameInstallation installation, string name = "", CancellationToken? token = null)
    {
        _logger.LogInformation("Indexing game files");
        var gameFiles = new HashSet<AModFile>();

        foreach (var (type, path) in installation.Locations)
        {
            await foreach (var result in FileHashCache.IndexFolder(path, token))
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
        _logger.LogInformation("Creating Loadout {Name}", name);
        var mod = new Mod
        {
            Name = "Game Files",
            Files = new EntityHashSet<AModFile>(_store, gameFiles.Select(g => g.Id)),
            Store = _store
        };
        
        var n = Loadout.Empty(_store) with
        {
            Installation = installation,
            Name = name, 
            Mods = new EntityHashSet<Mod>(_store, new [] {mod.Id})
        };
        _root.Alter(r => r with {Lists = r.Lists.With(n.LoadoutId, n)});
        
        _logger.LogInformation("Loadout {Name} {Id} created", name, n.LoadoutId);
        return new LoadoutMarker(this, n.LoadoutId);
    }

    public async Task<LoadoutMarker> InstallMod(LoadoutId LoadoutId, AbsolutePath path, string name, CancellationToken token = default)
    {
        var Loadout = GetLoadout(LoadoutId);
        
        var analyzed = (await _analyzer.AnalyzeFile(path, token) as AnalyzedArchive);

        var installer = _installers
            .Select(i => (Installer: i, Priority: i.Priority(Loadout.Value.Installation, analyzed.Contents)))
            .Where(p => p.Priority != Priority.None)
            .OrderBy(p => p.Priority)
            .FirstOrDefault();
        if (installer == default)
            throw new Exception($"No Installer found for {path}");

        var contents = installer.Installer.Install(Loadout.Value.Installation, analyzed.Hash, analyzed.Contents);

        name = string.IsNullOrWhiteSpace(name) ? path.FileName.ToString() : name;

        var newMod = new Mod()
        {
            Name = name,
            Files = new EntityHashSet<AModFile>(_store, contents.Select(c => c.Id)),
            Store = _store
        };
        Loadout.Add(newMod);
        return Loadout;
    }

    private LoadoutMarker GetLoadout(LoadoutId LoadoutId)
    {
        return new LoadoutMarker(this, LoadoutId);
    }


    public void Alter(LoadoutId id, Func<Loadout, Loadout> func, string changeMessage = "")
    {
        _root.Alter(r =>
        {
            var previousList = r.Lists[id];
            var newList = func(previousList)
                with
                {
                    LastModified = DateTime.UtcNow,
                    ChangeMessage = changeMessage,
                    PreviousVersion = previousList
                };
            return r with { Lists = r.Lists.With(newList.LoadoutId, newList) };
        });
    }

    public Loadout Get(LoadoutId id)
    {
        return _root.Value.Lists[id];
    }
}