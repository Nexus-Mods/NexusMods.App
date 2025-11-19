using System.Collections.Frozen;
using System.Collections.Immutable;
using GameFinder.Launcher.Heroic;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;
using NexusMods.Sdk.Games;

namespace NexusMods.Backend.Games.Locators;

internal class HeroicGOGLocator : IGameLocator
{
    private readonly ILogger _logger;
    private readonly HeroicGOGHandler _handler;
    private readonly FrozenDictionary<long, IGameData> _registeredGames;

    private static readonly GameStore Store = GameStore.GOG;

    public HeroicGOGLocator(IEnumerable<IGameData> games, ILoggerFactory loggerFactory, IFileSystem fileSystem)
    {
        _logger = loggerFactory.CreateLogger<HeroicGOGHandler>();

        _handler = new HeroicGOGHandler(
            fileSystem: fileSystem,
            logger: loggerFactory.CreateLogger<HeroicGOGHandler>()
        );

        _registeredGames = games
            .SelectMany(game => game.StoreIdentifiers.GOGProductIds, (game, storeIdentifier) => new KeyValuePair<long, IGameData>(storeIdentifier, game))
            .ToFrozenDictionary();
    }

    public IEnumerable<GameLocatorResult> Locate()
    {
        foreach (var result in _handler.FindAllGames())
        {
            if (result.TryPickT1(out var errorMessage, out var gameFinderGame))
            {
                _logger.LogWarning("Error locating games: {ErrorMessage}", errorMessage.Message);
                continue;
            }

            var storeIdentifier = gameFinderGame.Id.Value;
            _logger.LogDebug("Found game '{GameName}' with store identifier '{StoreIdentifier}'", gameFinderGame.Name, storeIdentifier);

            if (!_registeredGames.TryGetValue(storeIdentifier, out var game)) continue;

            var path = gameFinderGame.Path;
            var dlcIds = gameFinderGame.InstalledDLCs.Select(x => LocatorId.From(x.Value.ToString()));

            ImmutableArray<LocatorId> locatorIds =
            [
                LocatorId.From(gameFinderGame.BuildId.ToString()),
                ..dlcIds,
            ];

            yield return new GameLocatorResult
            {
                Game = game,
                Path = path,
                LocatorIds = locatorIds,
                StoreIdentifier = storeIdentifier.ToString(),
                Store = Store,
                Locator = this,
            };
        }
    }
}
