using System.Runtime.InteropServices;
using GameFinder.Launcher.Heroic;
using GameFinder.StoreHandlers.GOG;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.GOG;

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

    /// <inheritdoc/>
    public IEnumerable<GameLocatorResult> Find(ILocatableGame game)
    {
        if (game is not IGogGame tg) yield break;

        if (_cachedGames is null)
        {
            _cachedGames = _handler.FindAllGamesById(out var errors);
            if (errors.Length != 0)
            {
                foreach (var error in errors)
                    _logger.LogError("While looking for games: {Error}", error);
            }
        }

        foreach (var id in tg.GogIds)
        {
            if (!_cachedGames.TryGetValue(GOGGameId.From(id), out var found)) continue;
            var fs = found.Path.FileSystem;
            var gamePath = found.Path;

            if (found is HeroicGOGGame heroicGOGGame)
            {
                var wineData = heroicGOGGame.WineData;
                if (wineData is not null)
                {
                    if (wineData.WinePrefixPath.DirectoryExists())
                        fs = heroicGOGGame.GetWinePrefix()!.CreateOverlayFileSystem(fs);
                }

                // NOTE(erri120): GOG builds for Linux are whack, the installer Heroic uses is whack,
                // and this is a complete hack. See comments on https://github.com/Nexus-Mods/NexusMods.App/pull/2653
                // for details.
                if (heroicGOGGame.Platform == OSPlatform.Linux)
                    gamePath = gamePath.Combine("game");
            }

            yield return new GameLocatorResult(gamePath, fs, GameStore.GOG, new HeroicGOGLocatorResultMetadata
            {
                Id = id,
                BuildId = found.BuildId,
            });
        }
    }
}
