using GameFinder.Common;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Steam;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Interfaces;
using NexusMods.Interfaces.Components;
using NexusMods.Interfaces.StoreLocatorTags;
using NexusMods.Paths;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.StandardGameLocators.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddStandardGameLocators(false);
        container.AddSingleton<TemporaryFileManager>();
        container.AddSingleton<StubbedGame>();

        container.AddSingleton<AHandler<SteamGame, int>, StubbedSteamLocator>();
        container.AddSingleton<AHandler<GOGGame, long>, StubbedGogLocator>();
        container.AddAllSingleton<IModInstaller, StubbedGameInstaller>();
        container.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
    }
    
    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true;}));
}

public class StubbedGame : ISteamGame, IGogGame
{
    private readonly ILogger<StubbedGame> _logger;
    private readonly IEnumerable<IGameLocator> _locators;
    public string Name => "Stubbed Game";
    public string Slug => "stubbed-game";
    
    public static readonly RelativePath[] DATA_NAMES = new[]
    {
        "StubbedGame.exe",
        "config.ini",
        "Data/image.dds",
        "Models/model.3ds"
    }.Select(t => t.ToRelativePath()).ToArray();

    public static readonly Dictionary<RelativePath, (Hash Hash, Size Size)> DATA_CONTENTS = DATA_NAMES
        .ToDictionary(d => d, 
            d => (d.FileName.ToString().XxHash64(), (Size)d.FileName.ToString().Length));

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

    public IEnumerable<int> SteamIds => new [] {1};
    public IEnumerable<long> GogIds => new[] { (long)1 };
}


public class StubbedSteamLocator : AHandler<SteamGame, int>
{
    private readonly TemporaryFileManager _manager;
    private readonly TemporaryPath _folder;

    public StubbedSteamLocator(TemporaryFileManager manager)
    {
        _manager = manager;
        _folder = _manager.CreateFolder("steam_game");
    }
    public override IEnumerable<Result<SteamGame>> FindAllGames()
    {
        return new[]
        {
            new Result<SteamGame>(new SteamGame(1, "Stubbed Game", _folder.Path.ToString()), null)
        };
    }

    public override Dictionary<int, SteamGame> FindAllGamesById(out string[] errors)
    {
        errors = Array.Empty<string>();
        return FindAllGames().ToDictionary(g => g.Game!.AppId, v => v.Game)!;
    }
}

public class StubbedGogLocator : AHandler<GOGGame, long>
{
    private readonly TemporaryFileManager _manager;
    private readonly TemporaryPath _folder;

    public StubbedGogLocator(TemporaryFileManager manager)
    {
        _manager = manager;
        _folder = _manager.CreateFolder("gog_game");
    }
    public override IEnumerable<Result<GOGGame>> FindAllGames()
    {
        return new[]
        {
            new Result<GOGGame>(new GOGGame(1, "Stubbed Game", _folder.Path.ToString()), null)
        };
    }

    public override Dictionary<long, GOGGame> FindAllGamesById(out string[] errors)
    {
        errors = Array.Empty<string>();
        return FindAllGames().ToDictionary(g => g.Game!.Id, v => v.Game)!;
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
        return installation.Game is StubbedGame ? Interfaces.Priority.Normal : Interfaces.Priority.None;
    }

    public IEnumerable<AModFile> Install(GameInstallation installation, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        foreach (var (key, value) in files)
        {
            yield return new FromArchive
            {
                From = new HashRelativePath(srcArchive, key),
                To = new GamePath(GameFolderType.Game, key),
                Hash = value.Hash,
                Size = value.Size,
                Store = _store
            };
        }
    }
}