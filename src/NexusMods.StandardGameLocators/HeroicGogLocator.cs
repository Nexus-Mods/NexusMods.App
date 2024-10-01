using GameFinder.Launcher.Heroic;
using GameFinder.StoreHandlers.GOG;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.Games.Stores.GOG;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Find GOG games installed with the Heroic launcher.
/// </summary>
public class HeroicGogLocator : IGameLocator
{
    private readonly ILogger _logger;

    private readonly HeroicGOGHandler _handler;
    private IReadOnlyDictionary<GOGGameId, GOGGame>? _cachedGames;

    /// <summary>
    /// Constructor.
    /// </summary>
    public HeroicGogLocator(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<HeroicGogLocator>>();
        _handler = provider.GetRequiredService<HeroicGOGHandler>();
    }

    public IEnumerable<GameLocatorResult> Find(ILocatableGame game)
    {
        if (game is not IGogGame tg) yield break;

        if (_cachedGames is null)
        {
            _cachedGames = _handler.FindAllGamesById(out var errors);
            if (errors.Any())
            {
                foreach (var error in errors)
                    _logger.LogError("While looking for games: {Error}", error);
            }
        }

        foreach (var id in tg.GogIds)
        {
            if (!_cachedGames.TryGetValue(GOGGameId.From(id), out var found)) continue;
            // TODO: use WINE filesystem
            yield return new GameLocatorResult(found.Path, found.Path.FileSystem, GameStore.GOG, new HeroicGOGLocatorResultMetadata
            {
                Id = id,
            });
        }
    }
}
