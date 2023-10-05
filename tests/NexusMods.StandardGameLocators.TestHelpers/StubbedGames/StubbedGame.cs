using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Games.GameCapabilities.FolderMatchInstallerCapability;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.LoadoutSynchronizer;
using NexusMods.DataModel.ModInstallers;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
// ReSharper disable InconsistentNaming

namespace NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

public class StubbedGame : AGame, IEADesktopGame, IEpicGame, IOriginGame, ISteamGame, IGogGame, IXboxGame
{
    private readonly ILogger<StubbedGame> _logger;
    private readonly IEnumerable<IGameLocator> _locators;
    public override string Name => "Stubbed Game";
    public override GameDomain Domain => GameDomain.From("stubbed-game");

    public static readonly RelativePath[] DATA_NAMES = new[]
    {
        "StubbedGame.exe",
        "config.ini",
        "Data/image.dds",
        "Models/model.3ds"
    }.Select(t => t.ToRelativePath()).ToArray();

    public static readonly Dictionary<RelativePath, (Hash Hash, Size Size)> DATA_CONTENTS = DATA_NAMES
        .ToDictionary(d => d,
            d => (d.FileName.ToString().XxHash64AsUtf8(), Size.FromLong(d.FileName.ToString().Length)));

    private readonly IFileSystem _fileSystem;
    private Dictionary<AbsolutePath, DateTime> _modifiedTimes = new();
    private readonly IServiceProvider _serviceProvider;

    public StubbedGame(ILogger<StubbedGame> logger, IEnumerable<IGameLocator> locators,
        IFileSystem fileSystem, IServiceProvider provider) : base(locators)
    {
        _serviceProvider = provider;
        _logger = logger;
        _locators = locators;
        _fileSystem = fileSystem;
    }

    public override GamePath GetPrimaryFile(GameStore store) => new(LocationId.Game, "");

    public void ResetGameFolders()
    {
        // Delete all the folders
        foreach (var installation in Installations)
        {
            foreach (var (_, path) in installation.LocationsRegister.GetTopLevelLocations())
            {
                path.DeleteDirectory(true);
            }
        }

        // Re-create the folders/files
        foreach (var locator in _locators)
        {
            foreach (var result in locator.Find(this))
            {
                EnsureFiles(result.Path, LocationId.Game);
                EnsurePath(result.Path, LocationId.Preferences);
                EnsurePath(result.Path, LocationId.Saves);
            }
        }
    }

    public override IEnumerable<GameInstallation> Installations
    {
        get
        {
            _logger.LogInformation("Looking for {Game} in {Count} locators", ToString(), _locators.Count());
            return _locators.SelectMany(l => l.Find(this))
                .Select((i, idx) => new GameInstallation
                {
                    Game = this,
                    LocationsRegister = new GameLocationsRegister( new Dictionary<LocationId, AbsolutePath>()
                    {
                        { LocationId.Game, EnsureFiles(i.Path, LocationId.Game) },
                        { LocationId.Preferences, EnsurePath(i.Path, LocationId.Preferences) },
                        { LocationId.Saves, EnsurePath(i.Path, LocationId.Saves) },
                    }),
                    Version = Version.Parse($"0.0.{idx}.0"),
                    Store = GameStore.Unknown,
                });
        }
    }

    public override IEnumerable<AModFile> GetGameFiles(GameInstallation installation, IDataStore store)
    {
        return Array.Empty<AModFile>();
    }

    public override ValueTask<DiskState> GetInitialDiskState(GameInstallation installation)
    {
        var results = DATA_NAMES.Select(name =>
        {
            var gamePath = new GamePath(LocationId.Game, name);
            return KeyValuePair.Create(gamePath,
                new DiskStateEntry
                {
                    // This is coded to match what we write in `EnsureFile`
                    Size = Size.From((ulong)name.FileName.Path.Length),
                    Hash = name.FileName.Path.XxHash64AsUtf8(),
                    LastModified = _modifiedTimes[installation.LocationsRegister.GetResolvedPath(gamePath)]
                });
        });
        return ValueTask.FromResult(DiskState.Create(results));
    }


    public override ILoadoutSynchronizer Synchronizer
    {
        // Lazy initialization to avoid circular dependencies
        get { return new DefaultSynchronizer(_serviceProvider); }
    }

    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<StubbedGame>(
            "NexusMods.StandardGameLocators.TestHelpers.Resources.question_mark_game.png");

    public override IStreamFactory GameImage => throw new NotImplementedException("No game image for stubbed game.");
    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem,
        GameLocatorResult installation)
    {
        return new Dictionary<LocationId, AbsolutePath>()
            {
                { LocationId.Game, Installations.First().LocationsRegister[LocationId.Game] }
            };
    }

    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations) => new();

    private AbsolutePath EnsureFiles(AbsolutePath path, LocationId locationId)
    {
        lock (this)
        {
            path = path.Combine(locationId.ToString());
            path.CreateDirectory();
            foreach (var file in DATA_NAMES)
            {
                EnsureFile(path.Combine(file));
            }
            return path;
        }
    }

    private AbsolutePath EnsurePath(AbsolutePath path, LocationId locationId)
    {
        lock (this)
        {
            path = path.Combine(locationId.ToString());
            path.CreateDirectory();
            return path;
        }
    }

    private void EnsureFile(AbsolutePath path)
    {
        path.Parent.CreateDirectory();
        if (path.FileExists) return;
        _fileSystem.WriteAllText(path, path.FileName);
        _modifiedTimes[path] = DateTime.Now;
    }

    public IEnumerable<uint> SteamIds => new[] { 42u };
    public IEnumerable<long> GogIds => new[] { (long)42 };
    public IEnumerable<string> EADesktopSoftwareIDs => new[] { "ea-game-id" };
    public IEnumerable<string> EpicCatalogItemId => new[] { "epic-game-id" };
    public IEnumerable<string> OriginGameIds => new[] { "origin-game-id" };
    public IEnumerable<string> XboxIds => new[] { "xbox-game-id" };

    public override IEnumerable<IModInstaller> Installers => new IModInstaller[]
    {
        new StubbedGameInstaller()
    };
}
