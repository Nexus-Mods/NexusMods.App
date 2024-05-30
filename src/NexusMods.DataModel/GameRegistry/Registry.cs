using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.DataModel.GameRegistry;
using NexusMods.MnemonicDB;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Paths;

namespace NexusMods.DataModel;

/// <summary>
/// Game registry for all installed games.
/// </summary>
public class Registry : IGameRegistry, IHostedService
{
    private readonly IConnection _conn;


    private readonly SourceCache<GameInstallation, EntityId> _cache = new(x => x.GameMetadataId); 
    
    private readonly ReadOnlyObservableCollection<GameInstallation> _installedGames;
    private readonly ILogger<Registry> _logger;
    private readonly IEnumerable<IGame> _games;
    private readonly IEnumerable<IGameLocator> _locators;
    private readonly ConcurrentDictionary<EntityId, GameInstallation> _byId = new();
    private Task? _startupTask = null;

    /// <inheritdoc />
    public ReadOnlyObservableCollection<GameInstallation> InstalledGames => _installedGames;

    /// <inheritdoc />
    public IDictionary<EntityId, GameInstallation> Installations => _byId;

    /// <summary>
    /// Game registry for all installed games.
    /// </summary>
    public Registry(ILogger<Registry> logger, IEnumerable<ILocatableGame> games, IEnumerable<IGameLocator> locators, IConnection conn)
    {
        _games = games.OfType<IGame>().ToArray();
        _locators = locators;
        _logger = logger;
        _conn = conn;
        
        _cache
            .Connect()
            .Bind(out _installedGames)
            .Subscribe();
    }

    /// <summary>
    /// Registers external game installations, mostly used for testing, but it's a way to get a game installation
    /// from an arbitrary source.
    /// </summary>
    public async Task<GameInstallation> Register(ILocatableGame game, GameLocatorResult result, IGameLocator locator)
    {
        var id = await GetLocatorId(game, result);
        var install = ((IGame) game).InstallationFromLocatorResult(result, id, locator);
        _byId[install.GameMetadataId] = install;
        return install;
    }
    
    private async Task Startup(CancellationToken token)
    {
        await ((IHostedService)_conn).StartAsync(token);

        var results = await FindInstallations()
            .Distinct().ToArrayAsync(token);
        
        _cache.Edit(x => {
            x.Clear();
            foreach (var install in results)
                _cache.AddOrUpdate(install);
        });
    }

    /// <summary>
    /// Tries to get the locator result id from the database.
    /// </summary>
    private static bool TryGetLocatorResultId(IDb db, ILocatableGame locatableGame, GameLocatorResult result, [NotNullWhen(true)] out EntityId? id)
    {
        var found = db
            .FindIndexed(result.Path.ToString(), GameMetadata.Path)
            .Select(db.Get<GameMetadata.Model>)
            .FirstOrDefault(m => m.Domain == locatableGame.Domain && m.Store == result.Store);
        if (found is null)
        {
            id = null;
            return false;
        }
        id = found.Id;
        return true;
    }

    /// <summary>
    /// Gets the locator id from the database, if none exists it will be created and returned.
    /// </summary>
    private async ValueTask<EntityId> GetLocatorId(ILocatableGame locatableGame, GameLocatorResult result)
    {
        if (TryGetLocatorResultId(_conn.Db, locatableGame, result, out var id))
            return id.Value;
        
        using var tx = _conn.BeginTransaction();
        tx.Add(locatableGame, result, static (tx, db, game, result) =>
        {
            // Check for a race condition, someone may have added it before us.
            if (TryGetLocatorResultId(db, game, result, out var _))
                return;

            // Doesn't exist, so create it.
            _ = new GameMetadata.Model(tx)
            {
                Store = result.Store.Value,
                Domain = game.Domain.Value,
                Path = result.Path.ToString(),
            };
        });
        
        var txResult = await tx.Commit();
        
        if (!TryGetLocatorResultId(txResult.Db, locatableGame, result, out id))
            throw new InvalidOperationException("Failed to get locator result id after inserting it, this should never happen");
        
        return id.Value;
    }


    /// <summary>
    /// Looks through all locators to find all installations.
    /// </summary>
    private async IAsyncEnumerable<GameInstallation> FindInstallations()
    {
        foreach (var game in _games)
        {
            foreach (var locator in _locators)
            {
                foreach (var found in locator.Find(game))
                {
                    yield return await Register(game, found, locator);
                }
            }
        }
    }
    
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        lock (this)
        {
            _startupTask ??= Startup(cancellationToken);
        }
        await _startupTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
