using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
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

    public StubbedGame(ILogger<StubbedGame> logger, IEnumerable<IGameLocator> locators,
        IFileSystem fileSystem) : base(locators)
    {
        _logger = logger;
        _locators = locators;
        _fileSystem = fileSystem;
    }

    public override GamePath GetPrimaryFile(GameStore store) => new(GameFolderType.Game, "");

    public override IEnumerable<GameInstallation> Installations
    {
        get
        {
            _logger.LogInformation("Looking for {Game} in {Count} locators", ToString(), _locators.Count());
            return _locators.SelectMany(l => l.Find(this))
                .Select((i, idx) => new GameInstallation()
                {
                    Game = this,
                    LocationsRegister = new GameLocationsRegister( new Dictionary<GameFolderType, AbsolutePath>()
                    {
                        { GameFolderType.Game, EnsureFiles(i.Path) }
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

    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<StubbedGame>(
            "NexusMods.StandardGameLocators.TestHelpers.Resources.question_mark_game.png");

    public override IStreamFactory GameImage => throw new NotImplementedException("No game image for stubbed game.");
    protected override IReadOnlyDictionary<GameFolderType, AbsolutePath> GetLocations(IFileSystem fileSystem,
        GameLocatorResult installation)
    {
        return new Dictionary<GameFolderType, AbsolutePath>()
            {
                { GameFolderType.Game, Installations.First().LocationsRegister[GameFolderType.Game] }
            };
    }

    private AbsolutePath EnsureFiles(AbsolutePath path)
    {
        lock (this)
        {
            foreach (var file in DATA_NAMES)
            {
                EnsureFile(path.Combine(file));
            }
            return path;
        }
    }

    private void EnsureFile(AbsolutePath path)
    {
        path.Parent.CreateDirectory();
        if (path.FileExists) return;
        _fileSystem.WriteAllText(path, path.FileName);
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
