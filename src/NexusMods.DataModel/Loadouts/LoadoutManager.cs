using System.Collections.Immutable;
using System.IO.Compression;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.RateLimiting;
using NexusMods.DataModel.Sorting.Rules;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

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
    private readonly ILookup<GameDomain,ITool> _tools;

    public LoadoutManager(ILogger<LoadoutManager> logger,
        IResource<LoadoutManager, Size> limiter,
        ArchiveManager archiveManager,
        IEnumerable<IFileMetadataSource> metadataSources,
        IDataStore store, 
        FileHashCache fileHashCache, 
        IEnumerable<IModInstaller> installers, 
        FileContentsCache analyzer,
        IEnumerable<ITool> tools)
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
        _tools = tools.SelectMany(t => t.Domains.Select(d => (Tool: t, Domain: d)))
            .ToLookup(t => t.Domain, t => t.Tool);
    }
    
    public LoadoutManager Rebase(SqliteDataStore store)
    {
        return new LoadoutManager(_logger, Limiter, ArchiveManager, _metadataSources, store, FileHashCache, _installers,
            _analyzer, _tools.SelectMany(f => f));
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
            Files = new EntityDictionary<ModFileId, AModFile>(_store, gameFiles.Select(g => new KeyValuePair<ModFileId, Id>(g.Id, g.DataStoreId))),
            Store = _store,
            SortRules = ImmutableList<ISortRule<Mod, ModId>>.Empty.Add(new First<Mod, ModId>())
        };
        
        var n = Loadout.Empty(_store) with
        {
            Installation = installation,
            Name = name, 
            Mods = new EntityDictionary<ModId, Mod>(_store, new [] {new KeyValuePair<ModId,Id>(mod.Id, mod.DataStoreId)})
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
                    Id = ModFileId.New(),
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
        marker.Alter(mod.Id, m => m with {Files = m.Files.With(gameFiles, f => f.Id)});

        return marker;
    }

    
    private async IAsyncEnumerable<IModFileMetadata> GetMetadata(Loadout loadout, Mod mod, GameFile file,
        AnalyzedFile analyzed)
    {
        foreach (var source in _metadataSources)
        {
            if (!source.Games.Contains(loadout.Installation.Game.Domain))
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

        var analyzed = await _analyzer.AnalyzeFile(path, token);
        if (analyzed is not AnalyzedArchive archive)
        {
            var types = string.Join(", ", analyzed.FileTypes);
            throw new Exception($"Only archives are supported at the moment. {path} is not an archive. Types: {types}");
        }

        var installer = _installers
            .Select(i => (Installer: i, Priority: i.Priority(loadout.Value.Installation, archive.Contents)))
            .Where(p => p.Priority != Priority.None)
            .OrderBy(p => p.Priority)
            .FirstOrDefault();
        if (installer == default)
            throw new Exception($"No Installer found for {path}");

        var contents = installer.Installer.Install(loadout.Value.Installation, analyzed.Hash, archive.Contents);

        name = string.IsNullOrWhiteSpace(name) ? path.FileName.ToString() : name;

        var newMod = new Mod
        {
            Id = ModId.New(),
            Name = name,
            Files = new EntityDictionary<ModFileId, AModFile>(_store, contents.Select(c => new KeyValuePair<ModFileId, Id>(c.Id, c.DataStoreId))),
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

    public IEnumerable<ITool> Tools(GameDomain game)
    {
        return _tools[game];
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

    public async Task ExportTo(LoadoutId id, AbsolutePath output, CancellationToken token)
    {
        var loadout = Get(id);
        
        if (output.FileExists)
            output.Delete();
        using var zip = ZipFile.Open(output.ToString(), ZipArchiveMode.Create);


        var ids = loadout.Walk((state, itm) =>
        {
            state.Add(itm.DataStoreId);

            void AddFile(Hash hash, ISet<Id> hashes)
            {
                hashes.Add(new Id64(EntityCategory.FileAnalysis, (ulong)hash));
                foreach (var foundIn in _store.GetByPrefix<FileContainedIn>(new Id64(EntityCategory.FileContainedIn,
                             (ulong)hash)))
                {
                    hashes.Add(foundIn.DataStoreId);
                }
            }
            
            if (itm is AStaticModFile file)
            {
                AddFile(file.Hash, state);
            }
            if (itm is FromArchive archive)
            {
                AddFile(archive.From.Hash, state);
            }

            return state;
        }, new HashSet<Id>());
        
        _logger.LogDebug("Found {Count} entities to export", ids.Count);

        foreach (var entityId in ids)
        {
            var data = _store.GetRaw(entityId);
            if (data == null) continue;
            
            await using var entry = zip.CreateEntry("entities\\"+entityId.TaggedSpanHex, CompressionLevel.Optimal).Open();
            await entry.WriteAsync(data, token);
        }

        await using var rootEntry = zip.CreateEntry("root", CompressionLevel.Optimal).Open();
        await rootEntry.WriteAsync(Encoding.UTF8.GetBytes(loadout.DataStoreId.TaggedSpanHex), token);
    }

    public async Task<LoadoutMarker> ImportFrom(AbsolutePath path, CancellationToken token = default)
    {
        async ValueTask<(Id, byte[])> ProcessEntry(ZipArchiveEntry entry)
        {
            await using var es = entry.Open();
            using var ms = new MemoryStream();
            await es.CopyToAsync(ms, token);
            var id = Id.FromTaggedSpan(Convert.FromHexString(entry.Name));
            return (id, ms.ToArray());
        }

        using var zip = ZipFile.Open(path.ToString(), ZipArchiveMode.Read);
        var entityFolder = "entities".ToRelativePath();
        
        var entries = zip.Entries.Where(p => p.FullName.ToRelativePath().InFolder(entityFolder))
            .SelectAsync(ProcessEntry); 
        
        var loaded = await _store.PutRaw(entries, token);
        _logger.LogDebug("Loaded {Count} entities", loaded);
        
        await using var root = zip.GetEntry("root")!.Open();
        var rootId = Id.FromTaggedSpan(Convert.FromHexString(await root.ReadAllTextAsync(token)));

        var loadout = _store.Get<Loadout>(rootId);
        if (loadout == null)
            throw new Exception("Loadout not found after loading data store, the loadout may be corrupt");
        _root.Alter(r => r with { Lists = r.Lists.With(loadout.LoadoutId, loadout)});
        return new LoadoutMarker(this, loadout.LoadoutId);
    }
}