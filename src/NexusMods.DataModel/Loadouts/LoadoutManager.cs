using System.Collections.Immutable;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.RateLimiting;
using NexusMods.DataModel.Sorting;
using NexusMods.DataModel.Sorting.Rules;
using NexusMods.Interfaces;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts;

public class LoadoutManager
{
    private readonly ILogger<LoadoutManager> _logger;
    private readonly IDataStore _store;
    private readonly Root<LoadoutRegistry> _root;
    public readonly FileHashCache FileHashCache;
    public readonly ArchiveManager ArchiveManager;
    private readonly IModInstaller[] _installers;
    private readonly FileContentsCache _analyzer;
    private readonly IEnumerable<IFileMetadataSource> _metadataSources;

    public LoadoutManager(ILogger<LoadoutManager> logger,
        IResource<LoadoutManager, Size> limiter,
        ArchiveManager archiveManager,
        IEnumerable<IFileMetadataSource> metadataSources,
        IDataStore store, FileHashCache fileHashCache, IEnumerable<IModInstaller> installers, FileContentsCache analyzer)
    {
        _logger = logger;
        Limiter = limiter;
        ArchiveManager = archiveManager;
        _store = store;
        _root = new Root<LoadoutRegistry>(RootType.Loadouts, store);
        FileHashCache = fileHashCache;
        _metadataSources = metadataSources;
        _installers = installers.ToArray();
        _analyzer = analyzer;
    }

    public IResource<LoadoutManager,Size> Limiter { get; set; }

    public IObservable<LoadoutRegistry> Changes => _root.Changes.Select(r => r.New);
    public IEnumerable<LoadoutMarker> AllLoadouts => _root.Value.Lists.Values.Select(m => new LoadoutMarker(this, m.LoadoutId));

    public async Task<LoadoutMarker> ManageGame(GameInstallation installation, string name = "", CancellationToken token = default)
    {
        _logger.LogInformation("Indexing game files");
        var gameFiles = new HashSet<AModFile>();

        var mod = new Mod
        {
            Id = ModId.New(),
            Name = "Game Files",
            Files = new EntityHashSet<AModFile>(_store, gameFiles.Select(g => g.DataStoreId)),
            SortRules = ImmutableHashSet<ISortRule<Mod, ModId>>.Empty.Add(new First<Mod, ModId>()),
            Store = _store
        };
        
        var n = Loadout.Empty(_store) with
        {
            Installation = installation,
            Name = name, 
            Mods = new EntityHashSet<Mod>(_store, new [] {mod.DataStoreId})
        };
        
        _root.Alter(r => r with {Lists = r.Lists.With(n.LoadoutId, n)});
                
        _logger.LogInformation("Loadout {Name} {Id} created", name, n.LoadoutId);
        _logger.LogInformation("Adding game files");
        
        foreach (var (type, path) in installation.Locations)
        {
            await foreach (var result in FileHashCache.IndexFolder(path, token).WithCancellation(token))
            {
                var analysis = await _analyzer.AnalyzeFile(result.Path, token);
                var file = new GameFile
                {
                    To = new GamePath(type, result.Path.RelativeTo(path)),
                    Installation = installation,
                    Hash = result.Hash,
                    Size = result.Size,
                    Store = _store
                };
                
                var metaData = await GetMetadata(n, mod, file, analysis).ToHashSet();
                gameFiles.Add(file with {Metadata = metaData.ToImmutableHashSet()});
            }
        }
        gameFiles.AddRange(installation.Game.GetGameFiles(installation, _store));
        var marker = new LoadoutMarker(this, n.LoadoutId);
        marker.AlterMod(mod.Id, m => m with {Files = m.Files.With(gameFiles)});

        return marker;
    }

    
    private async IAsyncEnumerable<IModFileMetadata> GetMetadata(Loadout loadout, Mod mod, GameFile file,
        AnalyzedFile analyzed)
    {
        foreach (var source in _metadataSources)
        {
            if (!source.Games.Contains(loadout.Installation.Game.Slug))
                continue;
            if (!source.Extensions.Contains(file.To.Extension))
                continue;

            await foreach (var metadata in source.GetMetadata(loadout, mod, file, analyzed))
            {
                yield return metadata;
            }
        }

    }

    public async Task<(LoadoutMarker Loadout, ModId ModId)> InstallMod(LoadoutId LoadoutId, AbsolutePath path, string name, CancellationToken token = default)
    {
        var loadout = GetLoadout(LoadoutId);
        
        var analyzed = (await _analyzer.AnalyzeFile(path, token) as AnalyzedArchive)!;

        var installer = _installers
            .Select(i => (Installer: i, Priority: i.Priority(loadout.Value.Installation, analyzed.Contents)))
            .Where(p => p.Priority != Priority.None)
            .OrderBy(p => p.Priority)
            .FirstOrDefault();
        if (installer == default)
            throw new Exception($"No Installer found for {path}");

        var contents = installer.Installer.Install(loadout.Value.Installation, analyzed.Hash, analyzed.Contents);

        name = string.IsNullOrWhiteSpace(name) ? path.FileName.ToString() : name;

        var newMod = new Mod()
        {
            Id = ModId.New(),
            Name = name,
            Files = new EntityHashSet<AModFile>(_store, contents.Select(c => c.DataStoreId)),
            Store = _store
        };
        loadout.Add(newMod);
        return (loadout, newMod.Id);
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

    public void ReplaceFiles(LoadoutId id, List<(AModFile File, Mod Mod)> generated, string message)
    {
        var byMod = generated.GroupBy(x => x.Mod, x => x.File)
            .ToDictionary(x => x.Key);
        Alter(id, l =>
        {
            return l with
            {
                Mods = l.Mods.Keep(m =>
                {
                    if (!byMod.TryGetValue(m, out var files)) return m;
                    var indexed = files.ToDictionary(f => f.To);
                    return m with { Files = m.Files.Keep(f => indexed.GetValueOrDefault(f.To, f)) };
                })
            };
        }, message);
    }
}