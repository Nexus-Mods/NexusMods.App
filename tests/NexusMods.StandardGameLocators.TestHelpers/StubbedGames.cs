using GameFinder.Common;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators.TestHelpers;


public class StubbedGame : IEADesktopGame, IEpicGame, IOriginGame, ISteamGame, IGogGame
{
    private readonly ILogger<StubbedGame> _logger;
    private readonly IEnumerable<IGameLocator> _locators;
    public string Name => "Stubbed Game";
    public GameDomain Domain => GameDomain.From("stubbed-game");
    
    public static readonly RelativePath[] DATA_NAMES = new[]
    {
        "StubbedGame.exe",
        "config.ini",
        "Data/image.dds",
        "Models/model.3ds"
    }.Select(t => t.ToRelativePath()).ToArray();

    public static readonly Dictionary<RelativePath, (Hash Hash, Size Size)> DATA_CONTENTS = DATA_NAMES
        .ToDictionary(d => d, 
            d => (d.FileName.ToString().XxHash64(), Size.From(d.FileName.ToString().Length)));

    public StubbedGame(ILogger<StubbedGame> logger, IEnumerable<IGameLocator> locators)
    {
        _logger = logger;
        _locators = locators;
    }
    
    public IEnumerable<GameInstallation> Installations
    {
        get {
            _logger.LogInformation("Looking for {Game} in {Count} locators", this, _locators.Count());
            return _locators.SelectMany(l => l.Find(this))
                .Select((i, idx) => new GameInstallation()
                {
                    Game = this,
                    Locations = new Dictionary<GameFolderType, AbsolutePath>()
                    {
                        { GameFolderType.Game, EnsureFiles(i.Path) }
                    },
                    Version = Version.Parse($"0.0.{idx}.0")
                });
        }
    }

    public IEnumerable<AModFile> GetGameFiles(GameInstallation installation, IDataStore store)
    {
        return Array.Empty<AModFile>();
    }

    private AbsolutePath EnsureFiles(AbsolutePath path)
    {
        lock (this)
        {
            foreach (var file in DATA_NAMES)
            {
                EnsureFile(path.Join(file));
            }
            return path;
        }
    }

    private void EnsureFile(AbsolutePath path)
    {
        path.Parent.CreateDirectory();
        if (path.FileExists) return;
        File.WriteAllText(path.ToString(), path.FileName.ToString());
    }

    public IEnumerable<int> SteamIds => new [] {42};
    public IEnumerable<long> GogIds => new[] { (long)42 };
    public IEnumerable<string> EADesktopSoftwareIDs => new[] { "ea-game-id" };
    public IEnumerable<string> EpicCatalogItemId => new []{ "epic-game-id" };
    public IEnumerable<string> OriginGameIds => new []{ "origin-game-id" };
}

public class StubbedGameLocator<TGame, TId> : AHandler<TGame, TId> 
    where TGame : class where TId : notnull
{
    private readonly TemporaryFileManager _manager;
    private readonly TemporaryPath _folder;
    private readonly Func<TemporaryFileManager,TGame> _factory;
    private readonly Func<TGame,TId> _idSelector;
    private readonly TGame _game;

    public StubbedGameLocator(TemporaryFileManager manager, Func<TemporaryFileManager, TGame> factory, Func<TGame, TId> idSelector)
    {
        _manager = manager;
        _folder = _manager.CreateFolder("gog_game");
        _factory = factory;
        _idSelector = idSelector;
        _game = _factory(_manager);
    }
    public override IEnumerable<Result<TGame>> FindAllGames()
    {
        return new[]
        {
            Result.FromGame(_game)
        };
    }

    public override Dictionary<TId, TGame> FindAllGamesById(out string[] errors)
    {
        errors = Array.Empty<string>();
        return FindAllGames().ToDictionary(g => _idSelector(g.Game!), v => v.Game)!;
    }
}

public class StubbedGameInstaller : IModInstaller
{
    private readonly IDataStore _store;

    public StubbedGameInstaller(IDataStore store)
    {
        _store = store;
    }
    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        return installation.Game is StubbedGame ? Common.Priority.Normal : Common.Priority.None;
    }

    public IEnumerable<AModFile> Install(GameInstallation installation, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        foreach (var (key, value) in files)
        {
            yield return new FromArchive
            {
                Id = ModFileId.New(),
                From = new HashRelativePath(srcArchive, key),
                To = new GamePath(GameFolderType.Game, key),
                Hash = value.Hash,
                Size = value.Size,
                Store = _store
            };
        }
    }
}