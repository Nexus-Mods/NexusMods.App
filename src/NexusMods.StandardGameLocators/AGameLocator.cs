using GameFinder.Common;
using GameFinder.StoreHandlers.EADesktop;
using GameFinder.StoreHandlers.EGS;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Origin;
using GameFinder.StoreHandlers.Steam;
using GameFinder.StoreHandlers.Xbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Paths;
using IGame = NexusMods.Abstractions.Games.IGame;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Base class for an individual service used to locate installed games.
/// </summary>
/// <typeparam name="TGameType">The underlying game type library which maps to the <see cref="GameFinder"/> library. e.g. <see cref="SteamGame"/>.</typeparam>
/// <typeparam name="TId">Unique identifier used by the store for the games.</typeparam>
/// <typeparam name="TGame">Implementation of <see cref="IGame"/> such as <see cref="ISteamGame"/> that allows us to retrieve info about the game.</typeparam>
/// <typeparam name="TParent"></typeparam>
public abstract class AGameLocator<TGameType, TId, TGame, TParent> : IGameLocator
    where TGame : ILocatableGame
    where TParent : AGameLocator<TGameType, TId, TGame, TParent>
    where TGameType : class, GameFinder.Common.IGame
    where TId : notnull
{
    private readonly ILogger _logger;

    private readonly AHandler<TGameType, TId>? _handler;
    private IReadOnlyDictionary<TId, TGameType>? _cachedGames;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="provider"></param>
    protected AGameLocator(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<TParent>>();
        _handler = provider.GetService<AHandler<TGameType, TId>>();
    }

    /// <summary>
    /// Acquires all found copies of a given game.
    /// </summary>
    /// <param name="game">
    ///     The game to find.
    ///     We use the unique store identifiers from this game to locate results.
    /// </param>
    /// <returns>List of found game installations.</returns>
    public IEnumerable<GameLocatorResult> Find(ILocatableGame game)
    {
        if (game is not TGame tg) 
            yield break;

        if (_handler == null)
            yield break;
        
        if (_cachedGames is null)
        {
            _cachedGames = _handler.FindAllGamesById(out var errors);
            if (errors.Any())
            {
                foreach (var error in errors)
                    _logger.LogError("While looking for games: {Error}", error);
            }

#if DEBUG
            // Temporary hack to make it easy for contributors. To be removed in the future..
            foreach (var cachedGame in _cachedGames)
            {
                switch (cachedGame.Value)
                {
                    case XboxGame xb:
                        _logger.LogDebug($"Found Xbox Game: {xb.Id}, {xb.DisplayName}");
                        break;
                    case SteamGame st:
                        _logger.LogDebug($"Found Steam Game: {st.AppId}, {st.Name}");
                        break;
                    case EGSGame eg:
                        _logger.LogDebug($"Found Epic Game: {eg.CatalogItemId}, {eg.DisplayName}");
                        break;
                    case GOGGame gog:
                        _logger.LogDebug($"Found GOG Galaxy Game: {gog.Id}, {gog.Name}");
                        break;
                    case OriginGame og:
                        _logger.LogDebug($"Found Origin Game: {og.Id}, {og.InstallPath}");
                        break;
                    case EADesktopGame ea:
                        _logger.LogDebug($"Found EA Desktop Game: {ea.EADesktopGameId}, {ea.BaseInstallPath}");
                        break;
                }
            }
#endif
        }

        foreach (var id in Ids(tg))
        {
            if (!_cachedGames.TryGetValue(id, out var found)) continue;
            yield return new GameLocatorResult(Path(found), GetMappedFileSystem(found), Store, CreateMetadata(found));
        }
    }

    /// <summary>
    /// The <see cref="GameStore"/> associated with this <see cref="IGameLocator"/>.
    /// </summary>
    protected abstract GameStore Store { get; }

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

    protected virtual IFileSystem GetMappedFileSystem(TGameType game) => Path(game).FileSystem;

    /// <summary>
    /// Creates <see cref="IGameLocatorResultMetadata"/> for the specific result.
    /// </summary>
    /// <param name="game"></param>
    /// <returns></returns>
    protected abstract IGameLocatorResultMetadata CreateMetadata(TGameType game);
}
