using GameFinder.Common;
using GameFinder.StoreHandlers.Steam;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Base class for an individual service used to locate installed games.
/// </summary>
/// <typeparam name="TGameType">The underlying game type library which maps to the <see cref="GameFinder"/> library. e.g. <see cref="SteamGame"/>.</typeparam>
/// <typeparam name="TId">Unique identifier used by the store for the games.</typeparam>
/// <typeparam name="TGame">Implementation of <see cref="IGame"/> such as <see cref="ISteamGame"/> that allows us to retrieve info about the game.</typeparam>
public abstract class AGameLocator<TGameType, TId, TGame> : IGameLocator where TGameType : class
    where TGame : IGame
{
    private readonly ILogger _logger;
    private readonly AHandler<TGameType, TId> _handler;
    private IDictionary<TId, TGameType>? _cachedGames;

    /// <summary/>
    /// <param name="logger">Allows you to log results.</param>
    /// <param name="handler">Common interface for store handlers.</param>
    protected AGameLocator(ILogger logger, AHandler<TGameType, TId> handler)
    {
        _logger = logger;
        _handler = handler;
    }

    /// <summary>
    /// Acquires all found copies of a given game.
    /// </summary>
    /// <param name="game">
    ///     The game to find.
    ///     We use the unique store identifiers from this game to locate results.
    /// </param>
    /// <returns>List of found game installations.</returns>
    public IEnumerable<GameLocatorResult> Find(IGame game)
    {
        if (game is not TGame tg) yield break;

        if (_cachedGames is null)
        {
            _cachedGames = _handler.FindAllGamesById(out var errors);
            if (errors.Any())
            {
                foreach (var error in errors)
                    _logger.LogError("While looking for games: {Error}", error);
            }
        }

        foreach (var id in Ids(tg))
        {
            if (!_cachedGames.TryGetValue(id, out var found)) continue;
            yield return new GameLocatorResult(Path(found));
        }
    }

    /// <summary>
    /// Returns all unique identifiers for this game.
    /// </summary>
    /// <param name="game">The game to get the unique identifiers for.</param>
    /// <returns>All unique identifiers.</returns>
    protected abstract IEnumerable<TId> Ids(TGame game);

    /// <summary>
    /// Gets the path to the game's main installation folder.
    /// </summary>
    /// <param name="record">Absolute path to the folder storing the game.</param>
    /// <returns>Absolute path to game folder.</returns>
    protected abstract AbsolutePath Path(TGameType record);
}
