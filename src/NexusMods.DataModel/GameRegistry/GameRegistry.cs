using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using DynamicData;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.DataModel.GameRegistry;

/// <summary>
/// Game registry for all installed games.
/// </summary>
public class GameRegistry : IGameRegistry, IHostedService
{
    private readonly IConnection _conn;


    private readonly SourceCache<GameInstallation, EntityId> _cache = new(x => x.GameMetadataId); 
    
    private readonly ReadOnlyObservableCollection<GameInstallation> _installedGames;
    private readonly ILogger<GameRegistry> _logger;
    private readonly IGame[] _games;
    private readonly IGameLocator[] _locators;
    private readonly ConcurrentDictionary<EntityId, GameInstallation> _byId = new();
    private Task? _startupTask = null;

    /// <inheritdoc />
    public ReadOnlyObservableCollection<GameInstallation> InstalledGames => _installedGames;

    public ILocatableGame[] SupportedGames => _games;

    /// <inheritdoc />
    public IDictionary<EntityId, GameInstallation> Installations => _byId;

    /// <summary>
    /// Game registry for all installed games.
    /// </summary>
    public GameRegistry(ILogger<GameRegistry> logger, IEnumerable<ILocatableGame> games, IEnumerable<IGameLocator> locators, IConnection conn)
    {
        _games = games.OfType<IGame>().ToArray();
        _locators = locators.ToArray();
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
        try
        {
            var results = await FindInstallations()
                .Distinct()
                .ToArrayAsync(token);

            _cache.Edit(x =>
            {
                x.Clear();

                foreach (var install in results)
                {
                    _cache.AddOrUpdate(install);
                }
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception in startup");
        }
    }

    /// <summary>
    /// Tries to get the locator result id from the database.
    /// </summary>
    private static bool TryGetLocatorResultId(IDb db, ILocatableGame locatableGame, GameLocatorResult result, [NotNullWhen(true)] out EntityId? id)
    {
        var wasFound = GameInstallMetadata.FindByGameId(db, locatableGame.GameId)
            .TryGetFirst(m => m.Store == result.Store, out var found);
        if (!wasFound)
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

            // TX Functions don't yet support the .New() syntax, so we'll have to do it manually.
            var id = tx.TempId();
            tx.Add(id, GameInstallMetadata.Store, result.Store);
            tx.Add(id, GameInstallMetadata.GameId, game.GameId);
            tx.Add(id, GameInstallMetadata.Name, game.Name);
            tx.Add(id, GameInstallMetadata.Path, result.Path.ToString());
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
                using var enumerator = locator.Find(game).GetEnumerator();
                while (true)
                {
                    Task<GameInstallation> value;

                    try
                    {
                        if (!enumerator.MoveNext()) break;
                        var result = enumerator.Current;
                        value = Register(game, result, locator);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Exception in locator");
                        break;
                    }

                    yield return await value;
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
