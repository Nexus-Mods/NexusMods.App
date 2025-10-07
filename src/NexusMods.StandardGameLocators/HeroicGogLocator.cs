using System.Runtime.InteropServices;
using GameFinder.Launcher.Heroic;
using GameFinder.StoreHandlers.GOG;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Find GOG games installed with the Heroic launcher.
/// </summary>
public class HeroicGogLocator : IGameLocator
{
    private readonly ILogger _logger;

    private readonly HeroicGOGHandler _handler;
    private IReadOnlyDictionary<GOGGameId, HeroicGOGGame>? _cachedGames;

    /// <summary>
    /// Constructor.
    /// </summary>
    public HeroicGogLocator(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<HeroicGogLocator>>();
        _handler = provider.GetRequiredService<HeroicGOGHandler>();
    }

    /// <inheritdoc/>
    public IEnumerable<GameLocatorResult> Find(ILocatableGame game, bool forceRefreshCache = false)
    {
        if (game is not IGogGame tg) yield break;

        if (_cachedGames is null || forceRefreshCache)
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

            ILinuxCompatibilityDataProvider? linuxCompatibilityDataProvider = null;
            var targetOS = OSInformation.Shared;

            if (found is HeroicGOGGame heroicGOGGame)
            {
                targetOS = new OSInformation(heroicGOGGame.Platform);

                var wineData = heroicGOGGame.WineData;
                var winePrefix = heroicGOGGame.GetWinePrefix();
                if (wineData is not null && winePrefix is not null)
                {
                    var winePrefixPath = winePrefix.ConfigurationDirectory;
                    if (winePrefixPath.DirectoryExists())
                    {
                        fs = winePrefix.CreateOverlayFileSystem(fs);
                        linuxCompatibilityDataProvider = new LinuxCompatibilityDataProvider(winePrefixPath, wineData.EnvironmentVariables);
                    }
                }

                // NOTE(erri120): GOG builds for Linux are whack, the installer Heroic uses is whack,
                // and this is a complete hack. See comments on https://github.com/Nexus-Mods/NexusMods.App/pull/2653
                // for details.
                if (heroicGOGGame.Platform == OSPlatform.Linux)
                    gamePath = gamePath.Combine("game");
            }

            yield return new GameLocatorResult(gamePath, fs, 
                targetOS,
                GameStore.GOG,
                new HeroicGOGLocatorResultMetadata
                {
                    Id = id,
                    BuildId = found.BuildId,
                    LinuxCompatibilityDataProvider = linuxCompatibilityDataProvider,
                    // TODO: FIX THIS
                    DLCBuildIds = [],
                }
            );
        }
    }

    private class LinuxCompatibilityDataProvider : BaseLinuxCompatibilityDataProvider
    {
        private readonly string? _wineDllOverrides;

        public LinuxCompatibilityDataProvider(
            AbsolutePath winePrefixDirectoryPath,
            IReadOnlyDictionary<string, string> wineDataEnvironmentVariables) : base(winePrefixDirectoryPath)
        {
            wineDataEnvironmentVariables.TryGetValue(WineParser.WineDllOverridesEnvironmentVariableName, out _wineDllOverrides);
        }

        public override ValueTask<WineDllOverride[]> GetWineDllOverrides(CancellationToken cancellationToken)
        {
            if (_wineDllOverrides is null) return base.GetWineDllOverrides(cancellationToken);

            var result = WineParser.ParseEnvironmentVariable(_wineDllOverrides);
            return ValueTask.FromResult(result.ToArray());
        }
    }
}
