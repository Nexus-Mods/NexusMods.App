using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Loadouts;

namespace NexusMods.Backend.Games;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
internal class GameRegistry : IGameRegistry
{
    private readonly ILogger _logger;
    private readonly IConnection _connection;

    private readonly Lazy<IGameLocator[]> _locators;
    private readonly Lock _lock = new();

    private Optional<ImmutableArray<GameInstallation>> _cachedInstallations = Optional<ImmutableArray<GameInstallation>>.None;

    public GameRegistry(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<GameRegistry>>();
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _locators = new Lazy<IGameLocator[]>(() => serviceProvider.GetServices<IGameLocator>().ToArray());
    }

    public void ClearCache()
    {
        _cachedInstallations = Optional<ImmutableArray<GameInstallation>>.None;
    }

    public ImmutableArray<GameInstallation> LocateGameInstallations()
    {
        if (_cachedInstallations.HasValue) return _cachedInstallations.Value;

        lock (_lock)
        {
            if (_cachedInstallations.HasValue) return _cachedInstallations.Value;

            var results = new List<GameInstallation>();

            _logger.LogDebug("Using {Count} game locators", _locators.Value.Length);
            foreach (var locator in _locators.Value)
            {
                try
                {
                    foreach (var locatorResult in locator.Locate())
                    {
                        try
                        {
                            var resolvedLocations = locatorResult.Game.GetLocations(locatorResult.Path.FileSystem, locatorResult);
                            var gameLocations = GameLocations.Create(resolvedLocations);

                            var installation = new GameInstallation(locatorResult, gameLocations);
                            results.Add(installation);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Exception while locating a game with locator {Locator}", locator.GetType());
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception while locating games with locator {Locator}", locator.GetType());
                }
            }

            var installations = results.ToImmutableArray();
            _cachedInstallations = installations;

            _logger.LogInformation("Found {Count} game installations", installations.Length);
            return installations;
        }
    }

    public bool TryGetGameInstallation(Loadout.ReadOnly loadout, [NotNullWhen(true)] out GameInstallation? gameInstallation)
    {
        var metadata = loadout.Installation;
        foreach (var installation in LocateGameInstallations())
        {
            if (installation.Game.NexusModsGameId != metadata.GameId) continue;
            if (installation.LocatorResult.Store != metadata.Store) continue;

            gameInstallation = installation;
            return true;
        }

        gameInstallation = null;
        return false;
    }

    public bool TryGetMetadata(GameInstallation installation, out GameInstallMetadata.ReadOnly result)
    {
        // TODO: use game id instead of nexus mods game id
        var gameId = installation.Game.NexusModsGameId;
        if (!gameId.HasValue)
        {
            result = default(GameInstallMetadata.ReadOnly);
            return false;
        }

        var allMetadata = GameInstallMetadata.FindByGameId(_connection.Db, gameId.Value);
        foreach (var metadata in allMetadata)
        {
            if (metadata.Store != installation.LocatorResult.Store) continue;
            result = metadata;
            return true;
        }

        result = default(GameInstallMetadata.ReadOnly);
        return false;
    }
}
