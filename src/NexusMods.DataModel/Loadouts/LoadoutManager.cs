using System.Collections.Immutable;
using System.IO.Compression;
using System.Reactive.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
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

/// <summary>
/// Provides utility methods responsible for creating and modifying loadouts.
/// </summary>
public class LoadoutManager
{
    /// <summary>
    /// Provides access to a cache of file hashes for quick and easy access.
    /// This is used to speed up deployment [apply & ingest].
    /// </summary>
    public readonly FileHashCache FileHashCache;

    /// <summary>
    /// Provides lookup
    /// </summary>
    public readonly ArchiveManager ArchiveManager;

    private readonly ILogger<LoadoutManager> _logger;
    private readonly IDataStore _store;
    private readonly Root<LoadoutRegistry> _root;
    private readonly IModInstaller[] _installers;
    private readonly FileContentsCache _analyzer;
    private readonly IEnumerable<IFileMetadataSource> _metadataSources;
    private readonly ILookup<GameDomain, ITool> _tools;

    /// <summary/>
    /// <remarks>
    ///    This item is usually constructed using dependency injection (DI).
    /// </remarks>
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
        FileHashCache = fileHashCache;
        ArchiveManager = archiveManager;
        _logger = logger;
        Limiter = limiter;
        _store = store;
        _root = new Root<LoadoutRegistry>(RootType.Loadouts, store);
        _metadataSources = metadataSources;
        _installers = installers.ToArray();
        _analyzer = analyzer;
        _tools = tools.SelectMany(t => t.Domains.Select(d => (Tool: t, Domain: d)))
            .ToLookup(t => t.Domain, t => t.Tool);
    }

    /// <summary>
    /// Limits the number of concurrent jobs/threads [to not completely hog CPU]
    /// when needed.
    /// </summary>
    public IResource<LoadoutManager, Size> Limiter { get; set; }

    /// <summary>
    /// A list of all changes to the loadouts.
    ///
    /// Values here are published outside of the locking
    /// semantics and may thus be received out-of-order of a large number of
    /// updates are happening and once from multiple threads.
    ///
    /// See <see cref="Root{TRoot}.Changes"/> for more info.
    /// </summary>
    public IObservable<LoadoutRegistry> Changes => _root.Changes.Select(r => r.New);

    /// <summary>
    /// Returns a list of all currently user registered loadouts.
    /// </summary>
    public IEnumerable<LoadoutMarker> AllLoadouts => _root.Value.Lists.Values.Select(m => new LoadoutMarker(this, m.LoadoutId));

    /// <summary>
    /// Clones this loadout manager, used for operations analogous to `git rebase`.
    /// </summary>
    /// <param name="store">Data store to which we write to.</param>
    /// <remarks>
    ///    For now this just clones the current object; the actual rebase
    ///    functionality might not yet quite be here.
    /// </remarks>
    public LoadoutManager Rebase(SqliteDataStore store)
    {
        return new LoadoutManager(_logger, Limiter, ArchiveManager, _metadataSources, store, FileHashCache, _installers,
            _analyzer, _tools.SelectMany(f => f));
    }

    /// <summary>
    /// Brings a game instance/installation under management of the Nexus app
    /// such that it is tracked file-wise under a loadout.
    /// </summary>
    /// <param name="installation">Instance of the game on disk to newly manage.</param>
    /// <param name="name">Name of the newly created loadout.</param>
    /// <param name="token">Allows for cancelling the operation.</param>
    /// <returns></returns>
    /// <remarks>
    /// In the context of the Nexus app 'Manage Game' effectively means 'Add Game to App'; we call it
    /// 'Manage Game' because it effectively means putting the game files under our control.
    /// </remarks>
    public async Task<LoadoutMarker> ManageGameAsync(GameInstallation installation, string name = "", CancellationToken token = default)
    {
        _logger.LogInformation("Indexing game files");
        var gameFiles = new HashSet<AModFile>();

        var mod = new Mod
        {
            Id = ModId.New(),
            Name = "Game Files",
            Files = new EntityDictionary<ModFileId, AModFile>(_store, gameFiles.Select(g => new KeyValuePair<ModFileId, IId>(g.Id, g.DataStoreId))),
            Store = _store,
            SortRules = ImmutableList<ISortRule<Mod, ModId>>.Empty.Add(new First<Mod, ModId>())
        };

        var n = Loadout.Empty(_store) with
        {
            Installation = installation,
            Name = name,
            Mods = new EntityDictionary<ModId, Mod>(_store, new[] { new KeyValuePair<ModId, IId>(mod.Id, mod.DataStoreId) })
        };

        _root.Alter(r => r with { Lists = r.Lists.With(n.LoadoutId, n) });

        _logger.LogInformation("Loadout {Name} {Id} created", name, n.LoadoutId);
        _logger.LogInformation("Adding game files");

        foreach (var (type, path) in installation.Locations)
        {
            await foreach (var result in FileHashCache.IndexFolderAsync(path, token).WithCancellation(token))
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

                var metaData = await GetMetadata(n, mod, file, analysis).ToHashSetAsync();
                gameFiles.Add(file with { Metadata = metaData.ToImmutableHashSet() });
            }
        }
        gameFiles.AddRange(installation.Game.GetGameFiles(installation, _store));
        var marker = new LoadoutMarker(this, n.LoadoutId);
        marker.Alter(mod.Id, m => m with { Files = m.Files.With(gameFiles, f => f.Id) });

        return marker;
    }

    /// <summary>
    /// Installs a mod to a loadout with a given ID.
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <param name="path"></param>
    /// <param name="name"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <remarks>
    ///    For more details, consider reading <a href="https://github.com/Nexus-Mods/NexusMods.App/blob/main/docs/AddingAGame.md#mod-installation">Adding a Game</a>.
    /// </remarks>
    /// <exception cref="Exception">No supported installer.</exception>
    public async Task<(LoadoutMarker Loadout, ModId ModId)> InstallMod(LoadoutId loadoutId, AbsolutePath path, string name, CancellationToken token = default)
    {
        var loadout = GetLoadout(loadoutId);

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

        name = string.IsNullOrWhiteSpace(name) ? path.FileName : name;

        var newMod = new Mod
        {
            Id = ModId.New(),
            Name = name,
            Files = new EntityDictionary<ModFileId, AModFile>(_store, contents.Select(c => new KeyValuePair<ModFileId, IId>(c.Id, c.DataStoreId))),
            Store = _store
        };
        loadout.Add(newMod);
        return (loadout, newMod.Id);
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

            void AddFile(Hash hash, ISet<IId> hashes)
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
        }, new HashSet<IId>());

        _logger.LogDebug("Found {Count} entities to export", ids.Count);

        foreach (var entityId in ids)
        {
            var data = _store.GetRaw(entityId);
            if (data == null) continue;

            await using var entry = zip.CreateEntry("entities\\" + entityId.TaggedSpanHex, CompressionLevel.Optimal).Open();
            await entry.WriteAsync(data, token);
        }

        await using var rootEntry = zip.CreateEntry("root", CompressionLevel.Optimal).Open();
        await rootEntry.WriteAsync(Encoding.UTF8.GetBytes(loadout.DataStoreId.TaggedSpanHex), token);
    }

    public async Task<LoadoutMarker> ImportFrom(AbsolutePath path, CancellationToken token = default)
    {
        async ValueTask<(IId, byte[])> ProcessEntry(ZipArchiveEntry entry)
        {
            await using var es = entry.Open();
            using var ms = new MemoryStream();
            await es.CopyToAsync(ms, token);
            var id = IId.FromTaggedSpan(Convert.FromHexString(entry.Name.ToRelativePath().FileName));
            return (id, ms.ToArray());
        }

        using var zip = ZipFile.Open(path.ToString(), ZipArchiveMode.Read);
        var entityFolder = "entities".ToRelativePath();

        var entries = zip.Entries.Where(p => p.FullName.ToRelativePath().InFolder(entityFolder))
            .SelectAsync(ProcessEntry);

        var loaded = await _store.PutRaw(entries, token);
        _logger.LogDebug("Loaded {Count} entities", loaded);

        await using var root = zip.GetEntry("root")!.Open();
        var rootId = IId.FromTaggedSpan(Convert.FromHexString(await root.ReadAllTextAsync(token)));

        var loadout = _store.Get<Loadout>(rootId);
        if (loadout == null)
            throw new Exception("Loadout not found after loading data store, the loadout may be corrupt");
        _root.Alter(r => r with { Lists = r.Lists.With(loadout.LoadoutId, loadout) });
        return new LoadoutMarker(this, loadout.LoadoutId);
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

    private LoadoutMarker GetLoadout(LoadoutId loadoutId) => new(this, loadoutId);
}
