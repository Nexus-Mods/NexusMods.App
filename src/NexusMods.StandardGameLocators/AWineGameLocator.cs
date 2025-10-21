using GameFinder.Wine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// <see cref="IGameLocator"/> implementation that looks for games inside
/// wine prefixes.
/// </summary>
/// <typeparam name="TPrefix"></typeparam>
public abstract class AWineGameLocator<TPrefix> : IGameLocator
    where TPrefix : AWinePrefix
{
    private readonly ILogger _logger;
    private readonly IWinePrefixManager<TPrefix> _winePrefixManager;
    private readonly WineStoreHandlerWrapper _storeHandlerWrapper;

    private IReadOnlyList<GameFinder.Common.IGame>? _cachedGames;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="serviceProvider"></param>
    protected AWineGameLocator(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<AWineGameLocator<TPrefix>>>();
        _winePrefixManager = serviceProvider.GetRequiredService<IWinePrefixManager<TPrefix>>();
        _storeHandlerWrapper = serviceProvider.GetRequiredService<WineStoreHandlerWrapper>();
    }

    /// <inheritdoc/>
    public IEnumerable<GameLocatorResult> Find(ILocatableGame game, bool forceRefreshCache = false)
    {
        if (_cachedGames is null || forceRefreshCache)
            _cachedGames = FindAllGames();

        var foundGame = _storeHandlerWrapper.FindMatchingGame(_cachedGames, game);
        if (foundGame is not null) yield return foundGame;
    }

    private IReadOnlyList<GameFinder.Common.IGame> FindAllGames()
    {
        var results = new List<GameFinder.Common.IGame>();

        foreach (var res in _winePrefixManager.FindPrefixes())
        {
            if (!res.TryPickT0(out var prefix, out var error))
            {
                _logger.LogError("While looking for a Wine prefix: {Error}", error.Message);
                continue;
            }

            foreach (var handlerRes in _storeHandlerWrapper.FindAllGamesInPrefix(prefix))
            {
                if (!handlerRes.TryPickT0(out var game, out error))
                {
                    _logger.LogError("While looking for a game inside a Wine Prefix {Prefix}: {Error}", prefix, error.Message);
                    continue;
                }

                results.Add(game);
            }
        }

        return results;
    }
}
