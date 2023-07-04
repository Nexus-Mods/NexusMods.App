using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Interprocess.Jobs;
using NexusMods.DataModel.Interprocess.Messages;
using NexusMods.DataModel.Loadouts.Cursors;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.RateLimiting;
using NexusMods.DataModel.Sorting.Rules;
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
    public readonly IArchiveManager ArchiveManager;

    private readonly ILogger<LoadoutManager> _logger;

    private readonly IFileSystem _fileSystem;
    private readonly IModInstaller[] _installers;
    private readonly IArchiveAnalyzer _analyzer;
    private readonly ILookup<GameDomain, ITool> _tools;
    private readonly IInterprocessJobManager _jobManager;

    /// <summary/>
    /// <remarks>
    ///    This item is usually constructed using dependency injection (DI).
    /// </remarks>
    public LoadoutManager(ILogger<LoadoutManager> logger,
        IFileSystem fileSystem,
        LoadoutRegistry registry,
        IResource<LoadoutManager, Size> limiter,
        IArchiveManager archiveManager,
        IDataStore store,
        FileHashCache fileHashCache,
        IEnumerable<IModInstaller> installers,
        IArchiveAnalyzer analyzer,
        IEnumerable<ITool> tools,
        IInterprocessJobManager jobManager)
    {
        FileHashCache = fileHashCache;
        ArchiveManager = archiveManager;
        Registry = registry;
        _logger = logger;
        _fileSystem = fileSystem;
        Limiter = limiter;
        Store = store;
        _jobManager = jobManager;
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
    /// <param name="indexGameFiles">If false, will only add generated files and an otherwise empty mod, won't index or add files from the game folder. Useful for making faster tests that do not rely on game files directly</param>
    /// <param name="earlyReturn">If true, the function will return as soon as possible running indexing operations in the background, default is` false`</param>
    /// <returns></returns>
    /// <remarks>
    /// In the context of the Nexus app 'Manage Game' effectively means 'Add Game to App'; we call it
    /// 'Manage Game' because it effectively means putting the game files under our control.
    /// </remarks>
    public async Task<LoadoutMarker> ManageGameAsync(GameInstallation installation, string name = "",
        CancellationToken token = default,
        bool indexGameFiles = true,
        bool earlyReturn = false)
    {
        _logger.LogInformation("Indexing game files");

        var mod = new Mod
        {
            Id = ModId.New(),
            Status = ModStatus.Installing,
            Name = $"{installation.Game.Name} Files",
            Files = new EntityDictionary<ModFileId, AModFile>(Store),
            Version = installation.Version.ToString(),
            ModCategory = Mod.GameFilesCategory,
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

        var cursor = new ModCursor(loadoutId, mod.Id);

        _logger.LogInformation("Loadout {Name} {Id} created", name, loadoutId);

        var managementJob = InterprocessJob.Create(_jobManager, new ManageGameJob
        {
            LoadoutId = loadoutId
        });

        var indexTask =
            Task.Run(
                () => IndexAndAddGameFiles(installation, token, loadout,
                    mod, managementJob, indexGameFiles), token);

        if (!earlyReturn)
            await indexTask;

        return new LoadoutMarker(Registry, loadoutId);
    }

    private async Task IndexAndAddGameFiles(GameInstallation installation,
        CancellationToken token, Loadout loadout, Mod mod, IInterprocessJob managementJob, bool indexGameFiles)
    {
        // So we release this after the job is done.
        using var _ = managementJob;

        var gameFiles = new HashSet<AModFile>();
        _logger.LogInformation("Adding game files");

        managementJob.Progress = new Percent(0.0);

        if (indexGameFiles)
        {
            foreach (var (type, path) in installation.Locations)
            {
                if (!_fileSystem.DirectoryExists(path)) continue;

                await foreach (var result in FileHashCache
                                   .IndexFolderAsync(path, token)
                                   .WithCancellation(token))
                {
                    var analyzedFile = await _analyzer.AnalyzeFileAsync(result.Path, token);

                    var file = analyzedFile
                        .ToGameFile(new GamePath(type, result.Path.RelativeTo(path)), installation)
                        .WithPersist(Store);

                    gameFiles.Add(file);
                }
            }
        }

        managementJob.Progress = new Percent(0.5);
        gameFiles.AddRange(installation.Game.GetGameFiles(installation, Store));

        Registry.Alter(loadout.LoadoutId, mod.Id, "Add game files",
            m => m! with
            {
                Status = ModStatus.Installed,
                Enabled = true,
                Files = m.Files.With(gameFiles, f => f.Id)
            });

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
        return new LoadoutMarker(Registry, loadout.LoadoutId);
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

    private LoadoutMarker GetLoadout(LoadoutId loadoutId) => new(Registry, loadoutId);
}
