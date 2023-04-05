using System.Collections.Immutable;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Interprocess.Jobs;
using NexusMods.DataModel.Interprocess.Messages;
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
    /// Name of the root zip entry.
    /// </summary>
    public const string ZipRootName = "root";

    /// <summary>
    /// Name of the zip entry folder containing all entities.
    /// </summary>
    public const string ZipEntitiesName = "entities";

    /// <summary>
    /// Provides access to a cache of file hashes for quick and easy access.
    /// This is used to speed up deployment [apply and ingest].
    /// </summary>
    public readonly FileHashCache FileHashCache;

    /// <summary>
    /// Provides lookup within the 'Archives' folder, for existing installed mods
    /// etc.
    /// </summary>
    public readonly ArchiveManager ArchiveManager;

    private readonly ILogger<LoadoutManager> _logger;

    private readonly IModInstaller[] _installers;
    private readonly FileContentsCache _analyzer;
    private readonly IEnumerable<IFileMetadataSource> _metadataSources;
    private readonly ILookup<GameDomain, ITool> _tools;
    private readonly IInterprocessJobManager _jobManager;

    /// <summary/>
    /// <remarks>
    ///    This item is usually constructed using dependency injection (DI).
    /// </remarks>
    public LoadoutManager(ILogger<LoadoutManager> logger,
        LoadoutRegistry registry,
        IResource<LoadoutManager, Size> limiter,
        ArchiveManager archiveManager,
        IEnumerable<IFileMetadataSource> metadataSources,
        IDataStore store,
        FileHashCache fileHashCache,
        IEnumerable<IModInstaller> installers,
        FileContentsCache analyzer,
        IEnumerable<ITool> tools,
        IInterprocessJobManager jobManager)
    {
        FileHashCache = fileHashCache;
        ArchiveManager = archiveManager;
        Registry = registry;
        _logger = logger;
        Limiter = limiter;
        Store = store;
        _jobManager = jobManager;
        _metadataSources = metadataSources;
        _installers = installers.ToArray();
        _analyzer = analyzer;
        _tools = tools.SelectMany(t => t.Domains.Select(d => (Tool: t, Domain: d)))
            .ToLookup(t => t.Domain, t => t.Tool);
    }

    /// <summary>
    /// The loadout registry used by this manager
    /// </summary>
    public LoadoutRegistry Registry { get; }

    /// <summary>
    /// Limits the number of concurrent jobs/threads [to not completely hog CPU]
    /// when needed.
    /// </summary>
    public IResource<LoadoutManager, Size> Limiter { get; set; }

    /// <summary>
    /// Returns the data store.
    /// </summary>
    public IDataStore Store { get; }

    /// <summary>
    /// Brings a game instance/installation under management of the Nexus app
    /// such that it is tracked file-wise under a loadout.
    /// </summary>
    /// <param name="installation">Instance of the game on disk to newly manage.</param>
    /// <param name="name">Name of the newly created loadout.</param>
    /// <param name="token">Allows for cancelling the operation.</param>
    /// <param name="earlyReturn">If true, the function will return as soon as possible running indexing operations in the background, default is` false`</param>
    /// <returns></returns>
    /// <remarks>
    /// In the context of the Nexus app 'Manage Game' effectively means 'Add Game to App'; we call it
    /// 'Manage Game' because it effectively means putting the game files under our control.
    /// </remarks>
    public async Task<LoadoutMarker> ManageGameAsync(GameInstallation installation, string name = "", CancellationToken token = default, bool earlyReturn = false)
    {
        _logger.LogInformation("Indexing game files");

        var mod = new Mod
        {
            Id = ModId.New(),
            Name = $"{installation.Game.Name} Files",
            Files = new EntityDictionary<ModFileId, AModFile>(Store),
            Version = installation.Version.ToString(),
            ModCategory = "Game Files",
            SortRules = ImmutableList<ISortRule<Mod, ModId>>.Empty.Add(new First<Mod, ModId>())
        }.WithPersist(Store);

        var loadoutId = LoadoutId.Create();

        var loadout = Registry.Alter(loadoutId, $"Manage {installation.Game.Name}",
            l =>
            {
                return l with
                {
                    LoadoutId = loadoutId,
                    Installation = installation,
                    Name = name,
                    Mods = new EntityDictionary<ModId, Mod>(Store,
                        new[]
                        {
                            new KeyValuePair<ModId, IId>(mod.Id,
                                mod.DataStoreId)
                        })
                };
            });

        _logger.LogInformation("Loadout {Name} {Id} created", name, loadoutId);

        var managementJob = new InterprocessJob(JobType.ManageGame, _jobManager, loadoutId,
                $"Analyzing game files for {installation.Game.Name}");
        var indexTask = Task.Run(() => IndexAndAddGameFiles(installation, token, loadout, mod, managementJob), token);

        if (!earlyReturn)
            await indexTask;

        return new LoadoutMarker(this, loadoutId);
    }

    private async Task IndexAndAddGameFiles(GameInstallation installation,
        CancellationToken token, Loadout loadout, Mod mod, IInterprocessJob managementJob)
    {
        // So we release this after the job is done.
        using var _ = managementJob;

        var gameFiles = new HashSet<AModFile>();
        _logger.LogInformation("Adding game files");

        managementJob.Progress = new Percent(0.0);

        foreach (var (type, path) in installation.Locations)
        {
            await foreach (var result in FileHashCache.IndexFolderAsync(path, token)
                               .WithCancellation(token))
            {
                var analysis = await _analyzer.AnalyzeFileAsync(result.Path, token);
                var file = new GameFile
                {
                    Id = ModFileId.New(),
                    To = new GamePath(type, result.Path.RelativeTo(path)),
                    Installation = installation,
                    Hash = result.Hash,
                    Size = result.Size
                }.WithPersist(Store);

                var metaData =
                    await GetMetadata(loadout, mod, file, analysis).ToHashSetAsync();
                gameFiles.Add(
                    file with { Metadata = metaData.ToImmutableHashSet() });
            }
        }

        managementJob.Progress = new Percent(0.5);
        gameFiles.AddRange(installation.Game.GetGameFiles(installation, Store));

        Registry.Alter(loadout.LoadoutId, mod.Id, "Add game files",
            m => m! with { Files = m.Files.With(gameFiles, f => f.Id) });

    }

    /// <summary>
    /// Installs a mod to a loadout with a given ID.
    /// </summary>
    /// <param name="loadoutId">Index of the loadout to install a mod to.</param>
    /// <param name="path">Path of the archive to install.</param>
    /// <param name="name">Name of the mod being installed.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    /// <returns></returns>
    /// <remarks>
    ///    In the context of NMA, 'install' currently means, analyze archives and
    ///    run archive through installers.
    ///    For more details, consider reading <a href="https://github.com/Nexus-Mods/NexusMods.App/blob/main/docs/AddingAGame.md#mod-installation">Adding a Game</a>.
    /// </remarks>
    /// <exception cref="Exception">No supported installer.</exception>
    public async Task<(LoadoutMarker Loadout, ModId ModId)> InstallModAsync(LoadoutId loadoutId, AbsolutePath path, string name, CancellationToken token = default)
    {
        var loadout = GetLoadout(loadoutId);

        var analyzed = await _analyzer.AnalyzeFileAsync(path, token);
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

        var cts = new CancellationTokenSource();
        var contents = (await installer.Installer.GetFilesToExtractAsync(loadout.Value.Installation, analyzed.Hash, archive.Contents, cts.Token))
            .WithPersist(Store);

        name = string.IsNullOrWhiteSpace(name) ? path.FileName : name;

        var newMod = new Mod
        {
            Id = ModId.New(),
            Name = name,
            Files = new EntityDictionary<ModFileId, AModFile>(Store, contents.Select(c => new KeyValuePair<ModFileId, IId>(c.Id, c.DataStoreId)))
        };
        loadout.Add(newMod);
        return (loadout, newMod.Id);
    }

    /// <summary>
    /// Returns the available tools for a game.
    /// </summary>
    /// <param name="game">The game to get the tools for.</param>
    public IEnumerable<ITool> Tools(GameDomain game)
    {
        return _tools[game];
    }

    // TODO: Should probably provide this API for already grouped elements. https://github.com/Nexus-Mods/NexusMods.App/issues/211

    /// <summary>
    /// Replaces existing files for given mod(s) within the loadout/data store.
    /// Files to be replaced are matched using their <see cref="GamePath"/>(s).
    /// </summary>
    /// <param name="id">ID of the loadout to replace files in.</param>
    /// <param name="items">
    ///     List of files and their corresponding mods to perform the file replacement in.
    /// </param>
    /// <param name="message">The change message to attach to the new version of the loadout.</param>
    /// <remarks>
    ///    This is mostly used with 'generated' files, i.e. those output by a
    ///    game specific extension of NMA like RedEngine.
    ///
    ///    Does not add new files, only performs replacements.
    /// </remarks>
    public void ReplaceFiles(LoadoutId id, List<(AModFile File, Mod Mod)> items, string message)
    {
        var byMod = items.GroupBy(x => x.Mod, x => x.File)
            .ToDictionary(x => x.Key);

        Registry.Alter(id, message, l =>
        {
            return l with
            {
                Mods = l.Mods.Keep(m =>
                {
                    if (!byMod.TryGetValue(m, out var files))
                        return m; // not one of our mods

                    // This mod matches with one of our own provided ones for replacement.

                    // If the path of a given file matches one of our own, replace it with
                    // our own [the path therefore gets a new ID/Metadata/etc., and replacement is complete].
                    var indexed = files.ToDictionary(f => f.To);
                    return m with
                    {
                        Files = m.Files.Keep(f => indexed.GetValueOrDefault(f.To, f))
                    };
                })
            };
        });
    }

    /// <summary>
    /// Exports the contents of this loadout to a given directory.
    /// Exported loadout is zipped up.
    /// </summary>
    /// <param name="id">ID of the loadout to export.</param>
    /// <param name="output">The file path to export the loadout to.</param>
    /// <param name="token">Cancel operation with this.</param>
    /// <remarks></remarks>
    public async Task ExportToAsync(LoadoutId id, AbsolutePath output, CancellationToken token)
    {
        var loadout = Registry.Get(id)!;

        if (output.FileExists)
            output.Delete();

        using var zip = ZipFile.Open(output.ToString(), ZipArchiveMode.Create);
        var ids = loadout.Walk((state, itm) =>
        {
            state.Add(itm.DataStoreId);

            void AddFile(Hash hash, ISet<IId> hashes)
            {
                hashes.Add(new Id64(EntityCategory.FileAnalysis, (ulong)hash));
                foreach (var foundIn in Store.GetByPrefix<FileContainedIn>(new Id64(EntityCategory.FileContainedIn,
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
            var data = Store.GetRaw(entityId);
            if (data == null) continue;

            await using var entry = zip.CreateEntry($"{ZipEntitiesName}/" + entityId.TaggedSpanHex, CompressionLevel.Optimal).Open();
            await entry.WriteAsync(data, token);
        }

        await using var rootEntry = zip.CreateEntry(ZipRootName, CompressionLevel.Optimal).Open();
        await rootEntry.WriteAsync(Encoding.UTF8.GetBytes(loadout.DataStoreId.TaggedSpanHex), token);
    }

    /// <summary>
    /// Imports the contents of this loadout from a given directory [zip archive].
    /// </summary>
    /// <param name="path">Location of the file to import from.</param>
    /// <param name="token">Cancel operation with this.</param>
    /// <remarks></remarks>
    public async Task<LoadoutMarker> ImportFromAsync(AbsolutePath path, CancellationToken token = default)
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
        var entityFolder = ZipEntitiesName.ToRelativePath();

        var entries = zip.Entries.Where(p => p.FullName.ToRelativePath().InFolder(entityFolder))
            .SelectAsync(ProcessEntry);

        var loaded = await Store.PutRaw(entries, token);
        _logger.LogDebug("Loaded {Count} entities", loaded);

        await using var root = zip.GetEntry(ZipRootName)!.Open();
        var rootId = IId.FromTaggedSpan(Convert.FromHexString(await root.ReadAllTextAsync(token)));

        var loadout = Store.Get<Loadout>(rootId);
        if (loadout == null)
            throw new Exception("Loadout not found after loading data store, the loadout may be corrupt");
        Registry.Alter(loadout.LoadoutId, "Loadout Imported from backup",  _ => loadout);
        return new LoadoutMarker(this, loadout.LoadoutId);
    }

    /// <summary>
    /// Finds a free name for a new loadout. Will return a name like "My Loadout 1" or "My Loadout 2" etc.
    /// Will return a name like "My Loadout 1234-1234-1234-1234" if it can't find a free name.
    /// </summary>
    /// <param name="installation"></param>
    /// <returns></returns>
    public string FindName(GameInstallation installation)
    {
        var names = Registry.AllLoadouts().Select(l => l.Name).ToHashSet();
        for (var i = 1; i < 1000; i++)
        {
            var name = $"My Loadout {i}";
            if (!names.Contains(name))
                return name;
        }

        return $"My Loadout {Guid.NewGuid()}";
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

            await foreach (var metadata in source.GetMetadataAsync(loadout, mod, file, analyzed))
            {
                yield return metadata;
            }
        }
    }

    private LoadoutMarker GetLoadout(LoadoutId loadoutId) => new(this, loadoutId);
}
