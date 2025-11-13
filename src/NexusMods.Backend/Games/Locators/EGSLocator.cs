using System.Collections.Frozen;
using System.Collections.Immutable;
using GameFinder.StoreHandlers.EGS;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;
using NexusMods.Sdk.Games;

namespace NexusMods.Backend.Games.Locators;

internal class EGSLocator : IGameLocator
{
    private readonly ILogger _logger;
    private readonly EGSHandler _handler;
    private readonly FrozenDictionary<string, IGameData> _registeredGames;

    private static readonly GameStore Store = GameStore.EGS;

    public EGSLocator(IEnumerable<IGameData> games, ILoggerFactory loggerFactory, IFileSystem fileSystem, GameFinder.RegistryUtils.IRegistry registry)
    {
        _logger = loggerFactory.CreateLogger<EGSLocator>();

        _handler = new EGSHandler(registry, fileSystem);

        _registeredGames = games
            .SelectMany(game => game.StoreIdentifiers.EGSCatalogItemId, (game, storeIdentifier) => new KeyValuePair<string, IGameData>(storeIdentifier, game))
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

            var storeIdentifier = gameFinderGame.CatalogItemId.Value;
            _logger.LogDebug("Found game '{GameName}' with store identifier '{StoreIdentifier}'", gameFinderGame.DisplayName, storeIdentifier);

            if (!_registeredGames.TryGetValue(storeIdentifier, out var game)) continue;

            var path = gameFinderGame.InstallLocation;
            var locatorIds = gameFinderGame.ManifestHash.Select(LocatorId.From).ToImmutableArray();

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
