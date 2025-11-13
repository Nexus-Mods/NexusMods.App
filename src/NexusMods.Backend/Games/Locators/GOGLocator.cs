using System.Collections.Frozen;
using System.Collections.Immutable;
using GameFinder.StoreHandlers.GOG;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;
using NexusMods.Sdk.Games;

namespace NexusMods.Backend.Games.Locators;

internal class GOGLocator : IGameLocator
{
    private readonly ILogger _logger;
    private readonly GOGHandler _handler;
    private readonly FrozenDictionary<long, IGameData> _registeredGames;

    private static readonly GameStore Store = GameStore.GOG;

    public GOGLocator(IEnumerable<IGameData> games, ILoggerFactory loggerFactory, IFileSystem fileSystem, GameFinder.RegistryUtils.IRegistry registry)
    {
        _logger = loggerFactory.CreateLogger<GOGLocator>();

        _handler = new GOGHandler(
            registry: registry,
            fileSystem: fileSystem
        );

        _registeredGames = games
            .SelectMany(game => game.StoreIdentifiers.GOGProductIds, (game, storeIdentifier) => new KeyValuePair<long, IGameData>(storeIdentifier, game))
            .ToFrozenDictionary();
    }

    public IEnumerable<GameLocatorResult> Locate()
    {
        var gameFinderGames = new List<GOGGame>();

        foreach (var result in _handler.FindAllGames())
        {
            if (result.TryPickT1(out var errorMessage, out var gameFinderGame))
            {
                _logger.LogWarning("Error locating games: {ErrorMessage}", errorMessage.Message);
                continue;
            }

            gameFinderGames.Add(gameFinderGame);
        }

        foreach (var gameFinderGame in gameFinderGames)
        {
            var storeIdentifier = gameFinderGame.Id.Value;
            _logger.LogDebug("Found game '{GameName}' with store identifier '{StoreIdentifier}'", gameFinderGame.Name, storeIdentifier);

            if (!_registeredGames.TryGetValue(storeIdentifier, out var game)) continue;

            var path = gameFinderGame.Path;

            var dlcIds = gameFinderGames
                .Where(x => x.ParentGameId == gameFinderGame.Id)
                .Select(x => LocatorId.From(x.Id.Value.ToString()));

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
