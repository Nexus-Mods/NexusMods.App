using GameFinder.Common;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Steam;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        container.AddSingleton<StubbedGame>();

        container.AddSingleton<AHandler<SteamGame, int>, StubbedSteamLocator>();
        container.AddSingleton<AHandler<GOGGame, long>, StubbedGogLocator>();
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
                .Select(i => new GameInstallation()
                {
                    Game = this,
                    Locations = new Dictionary<GameFolderType, AbsolutePath>()
                    {
                        { GameFolderType.Game, i.Path }
                    },
                    Version = Version.Parse("0.0.1.0")
                });
        }
    }
    public IEnumerable<int> SteamIds => new [] {1};
    public IEnumerable<long> GogIds => new[] { (long)1 };
}


public class StubbedSteamLocator : AHandler<SteamGame, int>
{
    public override IEnumerable<Result<SteamGame>> FindAllGames()
    {
        return new[]
        {
            new Result<SteamGame>(new SteamGame(1, "Stubbed Game", @"c:\games\steam_game\1"), null)
        };
    }

    public override Dictionary<int, SteamGame> FindAllGamesById(out string[] errors)
    {
        errors = Array.Empty<string>();
        return new Dictionary<int, SteamGame>
        {
            {1, new SteamGame(1, "Stubbed Game", @"c:\games\steam_game\1")}
        };
    }
}

public class StubbedGogLocator : AHandler<GOGGame, long>
{
    public override IEnumerable<Result<GOGGame>> FindAllGames()
    {
        return new[]
        {
            new Result<GOGGame>(new GOGGame(1, "Stubbed Game", @"c:\games\gog_game\1"), null)
        };
    }

    public override Dictionary<long, GOGGame> FindAllGamesById(out string[] errors)
    {
        errors = Array.Empty<string>();
        return new Dictionary<long, GOGGame>
        {
            {1, new GOGGame(1, "Stubbed Game", @"c:\games\gog_game\1")}
        };
    }
}