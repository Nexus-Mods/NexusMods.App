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
    protected readonly ILogger Logger;

    private readonly AHandler<TGameType, TId>? _handler;
    private IReadOnlyDictionary<TId, TGameType>? _cachedGames;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="provider"></param>
    protected AGameLocator(IServiceProvider provider)
    {
        Logger = provider.GetRequiredService<ILogger<TParent>>();
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
    public IEnumerable<GameLocatorResult> Find(ILocatableGame game, bool forceRefreshCache = false)
    {
        if (game is not TGame tg) 
            yield break;

        if (_handler == null)
            yield break;
        
        if (_cachedGames is null || forceRefreshCache)
        {
            _cachedGames = _handler.FindAllGamesById(out var errors);
            if (errors.Any())
            {
                foreach (var error in errors)
                    Logger.LogError("While looking for games: {Error}", error);
            }

            foreach (var cachedGame in _cachedGames)
            {
                switch (cachedGame.Value)
                {
                    case XboxGame xb:
                        Logger.LogInformation($"Found Xbox Game: {xb.Id}, {xb.DisplayName}");
                        break;
                    case SteamGame st:
                        Logger.LogInformation($"Found Steam Game: {st.AppId}, {st.Name}");
                        break;
                    case EGSGame eg:
                        Logger.LogInformation($"Found Epic Game: {eg.CatalogItemId}, {eg.DisplayName}");
                        break;
                    case GOGGame gog:
                        Logger.LogInformation($"Found GOG Galaxy Game: {gog.Id}, {gog.Name}");
                        break;
                    case OriginGame og:
                        Logger.LogInformation($"Found Origin Game: {og.Id}, {og.InstallPath}");
                        break;
                    case EADesktopGame ea:
                        Logger.LogInformation($"Found EA Desktop Game: {ea.EADesktopGameId}, {ea.BaseInstallPath}");
                        break;
                }
                var metadata = CreateMetadata(cachedGame.Value, _cachedGames.Values);
                foreach (var id in metadata.ToLocatorIds())
                {
                    Logger.LogInformation($" - ID: {id}");
                }
            }
        }

        foreach (var id in Ids(tg))
        {
            if (!_cachedGames.TryGetValue(id, out var found)) continue;
            yield return new GameLocatorResult(
                Path(found),
                GetMappedFileSystem(found),
                GetTargetOS(found),
                Store,
                CreateMetadata(found, _cachedGames.Values)
            );
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

    protected virtual IOSInformation GetTargetOS(TGameType game) => OSInformation.Shared;

    /// <summary>
    /// Creates <see cref="IGameLocatorResultMetadata"/> for the specific result.
    /// </summary>
    /// <param name="game">the game data that was find by the GameFinder library</param>
    /// <param name="otherFoundGames">all the other games found in the same store by the GameFinder library, this is most often
    /// used to locate DLC installed into the game folder along with the game</param>
    /// <returns></returns>
    protected abstract IGameLocatorResultMetadata CreateMetadata(TGameType game, IEnumerable<TGameType> otherFoundGames);
}
