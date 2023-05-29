using System.Collections.Immutable;
using System.Diagnostics;
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
using NexusMods.DataModel.Loadouts.Cursors;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.Loadouts.Mods;
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
    public readonly IArchiveManager ArchiveManager;

    private readonly ILogger<LoadoutManager> _logger;

    private readonly IFileSystem _fileSystem;
    private readonly IModInstaller[] _installers;
    private readonly IArchiveAnalyzer _analyzer;
    private readonly IEnumerable<IFileMetadataSource> _metadataSources;
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
        IEnumerable<IFileMetadataSource> metadataSources,
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

        var mod = new Mods.Mod
        {
            Id = ModId.New(),
            Status = ModStatus.Installing,
            Name = $"{installation.Game.Name} Files",
            Files = new EntityDictionary<ModFileId, AModFile>(Store),
            Version = installation.Version.ToString(),
            ModCategory = Mod.GameFilesCategory,
            SortRules = ImmutableList<ISortRule<Mods.Mod, ModId>>.Empty.Add(new First<Mods.Mod, ModId>())
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
                    Mods = new EntityDictionary<ModId, Mods.Mod>(Store,
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
        CancellationToken token, Loadout loadout, Mods.Mod mod, IInterprocessJob managementJob, bool indexGameFiles)
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
                    var analysis =
                        await _analyzer.AnalyzeFileAsync(result.Path, token);
                    var file = new GameFile
                    {
                        Id = ModFileId.New(),
                        To = new GamePath(type, result.Path.RelativeTo(path)),
                        Installation = installation,
                        Hash = result.Hash,
                        Size = result.Size
                    }.WithPersist(Store);

                    var metaData =
                        await GetMetadata(loadout, mod, file, analysis)
                            .ToHashSetAsync();
                    gameFiles.Add(
                        file with { Metadata = metaData.ToImmutableHashSet() });
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
                Installed = DateTime.UtcNow,
                Files = m.Files.With(gameFiles, f => f.Id)
            });

    }

    /// <summary>
    /// Installs the mods found in the archive to the loadout.
    /// </summary>
    /// <param name="loadoutId">Id of the loadout to install the mods to.</param>
    /// <param name="archivePath">Archive to analyze and install the mods from.</param>
    /// <param name="defaultModName">Default name of the mod. This is optional and can be overwritten by the installers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<(LoadoutMarker Loadout, ModId[] ModIds)> InstallModsFromArchiveAsync(
        LoadoutId loadoutId,
        AbsolutePath archivePath,
        string? defaultModName = null,
        CancellationToken cancellationToken = default)
    {
        // Get the loadout and create the mod so we can use it in the job.
        var loadout = GetLoadout(loadoutId);

        var baseMod = new Mods.Mod
        {
            Id = ModId.New(),
            Name = string.IsNullOrWhiteSpace(defaultModName) ? archivePath.FileName : defaultModName,
            Files = new EntityDictionary<ModFileId, AModFile>(Store),
            Status = ModStatus.Installing
        };

        var cursor = new ModCursor { LoadoutId = loadoutId, ModId = baseMod.Id };
        loadout.Add(baseMod);

        try
        {
            // Create the job so the UI can show progress.
            using var job = InterprocessJob.Create(_jobManager, new AddModJob
            {
                ModId = baseMod.Id,
                LoadoutId = loadoutId
            });
            
            // Step 1: Analyze the archive.
            var analyzed = await _analyzer.AnalyzeFileAsync(archivePath, cancellationToken);
            if (analyzed is not AnalyzedArchive archive)
            {
                var types = string.Join(", ", analyzed.FileTypes);
                throw new NotSupportedException($"Only archives are supported at the moment. {archivePath} is not an archive. Types: {types}");
            }

            job.Progress = new Percent(0.50);

            // Step 2: Run the archive through the installers.
            var installer = _installers
                .Select(i => (Installer: i, Priority: i.GetPriority(loadout.Value.Installation, archive.Contents)))
                .Where(p => p.Priority != Priority.None)
                .OrderBy(p => p.Priority)
                .FirstOrDefault();

            if (installer == default)
            {
                _logger.LogError("No Installer found for {Path}", archivePath);
                Registry.Alter(cursor, $"Failed to install mod {archivePath}",m => m! with { Status = ModStatus.Failed });
                throw new NotSupportedException($"No Installer found for {archivePath}");
            }

            // Step 3: Install the mods.
            var results = await installer.Installer.GetModsAsync(
                loadout.Value.Installation,
                baseMod.Id,
                analyzed.Hash,
                archive.Contents,
                cancellationToken);

            var mods = results.Select(result => new Mods.Mod
            {
                Id = result.Id,
                Files = result.Files.ToEntityDictionary(Store),
                Name = result.Name ?? baseMod.Name,
                Version = result.Version ?? baseMod.Version,
            }).WithPersist(Store).ToArray();

            job.Progress = new Percent(0.75);

            // Step 4: Add the mod to the loadout.
            AModMetadata? modMetadata = mods.Length > 1
                ? new GroupMetadata
                {
                    Id = GroupId.New(),
                    CreationReason = GroupCreationReason.MultipleModsOneArchive
                }
                : null;

            if (mods.Length == 0)
            {
                Registry.Alter(cursor, $"Failed to install mod {archivePath}", m => m! with { Status = ModStatus.Failed});
                throw new NotImplementedException($"The installer returned 0 mods for {archivePath}");
            }

            var modIds = mods.Select(mod => mod.Id).ToHashSet();
            Debug.Assert(modIds.Count == mods.Length, $"The installer {installer.Installer.GetType()} returned mods with non-unique ids.");

            foreach (var mod in mods)
            {
                mod.Metadata = modMetadata;

                if (mod.Id.Equals(baseMod.Id))
                {
                    Registry.Alter(
                        cursor,
                        $"Adding mod files to {baseMod.Name}",
                        x => x! with
                        {
                            Status = ModStatus.Installed,
                            Enabled = true,
                            Name = mod.Name,
                            Files = mod.Files,
                            Metadata = mod.Metadata
                        });
                }
                else
                {
                    loadout.Add(mod with
                    {
                        Status = ModStatus.Installed,
                        Enabled = true,
                    });
                }
            }

            if (!modIds.Contains(baseMod.Id))
            {
                loadout.Remove(baseMod);
            }

            job.Progress = Percent.One;

            return (loadout, mods.Select(x => x.Id).ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install mod {Path}", archivePath);
            Registry.Alter(cursor, $"Failed to install {archivePath}", mod => mod! with
            {
                Status = ModStatus.Failed
            });

            throw;
        }
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

    private LoadoutMarker GetLoadout(LoadoutId loadoutId) => new(Registry, loadoutId);
}
