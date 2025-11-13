using System.Collections.Frozen;
using System.Collections.Immutable;
using GameFinder.StoreHandlers.Xbox;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;
using NexusMods.Sdk.Games;

namespace NexusMods.Backend.Games.Locators;

internal class XboxLocator : IGameLocator
{
    private readonly ILogger _logger;
    private readonly XboxHandler _handler;
    private readonly FrozenDictionary<string, IGameData> _registeredGames;

    private static readonly GameStore Store = GameStore.XboxGamePass;

    public XboxLocator(IEnumerable<IGameData> games, ILoggerFactory loggerFactory, IFileSystem fileSystem)
    {
        _logger = loggerFactory.CreateLogger<XboxLocator>();

        _handler = new XboxHandler(fileSystem);

        _registeredGames = games
            .SelectMany(game => game.StoreIdentifiers.XboxPackageIdentifiers, (game, storeIdentifier) => new KeyValuePair<string, IGameData>(storeIdentifier, game))
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
            _logger.LogDebug("Found game '{GameName}' with store identifier '{StoreIdentifier}'", gameFinderGame.DisplayName, storeIdentifier);

            if (!_registeredGames.TryGetValue(storeIdentifier, out var game)) continue;

            var path = gameFinderGame.Path;
            ImmutableArray<LocatorId> locatorIds = [LocatorId.From(storeIdentifier)];

            yield return new GameLocatorResult
            {
                Game = game,
                Path = path,
                LocatorIds = locatorIds,
                StoreIdentifier = storeIdentifier,
                Store = Store,
                Locator = this,
            };
        }
    }
}
