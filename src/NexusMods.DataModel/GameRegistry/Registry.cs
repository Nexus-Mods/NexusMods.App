using System.Collections.ObjectModel;
using System.Diagnostics;
using DynamicData;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.DataModel.GameRegistry;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel;

/// <summary>
/// Game registry for all installed games.
/// </summary>
public class Registry : IGameRegistry, IHostedService
{
    private readonly IConnection _conn;
    private Dictionary<EntityId,GameInstallation> _byId = new();
    private Dictionary<(GameDomain Domain, Version Version, GameStore Store),EntityId> _byInstall = new();

    private readonly SourceCache<(GameInstallation Game, EntityId Id), EntityId> _cache = new(x => x.Id); 
    
    private readonly ReadOnlyObservableCollection<GameInstallation> _installedGames;
    private readonly ILogger<Registry> _logger;
    private readonly IEnumerable<ILocatableGame> _games;
    private bool _isInitialized;
    
#if DEBUG
    private int _initThreadId;
#endif

    /// <inheritdoc />
    public ReadOnlyObservableCollection<GameInstallation> InstalledGames => _installedGames;

    /// <summary>
    /// Game registry for all installed games.
    /// </summary>
    public Registry(ILogger<Registry> logger, IEnumerable<ILocatableGame> games, IConnection conn)
    {
        _games = games;
        _logger = logger;
        _conn = conn;

        _cache
            .Connect()
            .Transform(g => g.Game)
            .Bind(out _installedGames)
            .Subscribe();
    }

    private async Task Startup(IEnumerable<ILocatableGame> games)
    {
#if DEBUG
        _initThreadId = Environment.CurrentManagedThreadId;
#endif
        var allInstalls = from game in games
            let igame = (IGame)game
            from install in igame.Installations
            select install;
        
        _logger.LogInformation("Getting game metadata");
        
        var allInDb = GameMetadata
            .All(_conn.Db)
            .ToDictionary(x => (GameDomain.From(x.Domain), GameStore.From(x.Store)));

        _logger.LogInformation("Creating transaction");
        using var tx = _conn.BeginTransaction();

        var added = new List<(EntityId Id, GameInstallation)>();
        var results = new List<(EntityId Id, GameInstallation)>();
        foreach (var install in allInstalls)
        {
            if (allInDb.TryGetValue((install.Game.Domain, install.Store), out var found))
            {
                results.Add((found.Id, install));
            }
            else
            {
                var meta = new GameMetadata.Model(tx)
                {
                    Domain = install.Game.Domain.Value,
                    Store = install.Store.Value,
                };
                added.Add((meta.Id, install));
            }
        }

        if (added.Count > 0)
        {
            _logger.LogInformation("Found {Count} new games to register", added.Count);
            var result = await tx.Commit();
            _logger.LogInformation("Registered {Count} new games", added.Count);
            foreach (var (id, install) in added)
                results.Add((result[id], install));
        }
        
        _logger.LogInformation("Register setup");

        _byId = results.ToDictionary(x => x.Id, x => x.Item2);
        _byInstall = results.ToDictionary(x => GetKey(x.Item2), x => x.Id);
        
        _cache.Edit(x => {
            x.Clear();
            foreach (var (id, install) in results)
                _cache.AddOrUpdate((install, id));
        });
        _isInitialized = true;
    }
    
    private (GameDomain Domain, Version Version, GameStore Store) GetKey(GameInstallation installation)
    {
        return (installation.Game.Domain, installation.Version, installation.Store);
    }

    /// <inheritdoc />
    public IEnumerable<GameInstallation> AllInstalledGames
    {
        get
        {
            WaitUntilInitialized();
            return _byId.Values;
        }
    }

    /// <inheritdoc />
    public GameInstallation Get(EntityId id)
    {
        WaitUntilInitialized();
        return _byId[id];
    }

    /// <inheritdoc />
    public EntityId GetId(GameInstallation installation)
    {
        WaitUntilInitialized();
        return _byInstall[GetKey(installation)];
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ((IHostedService)_conn).StartAsync(cancellationToken);
        await Startup(_games);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    private void WaitUntilInitialized()
    {
        /*
            Note(sewer)
            
            This is an IHostedService.
            It will be initialized in a background threadpool thread.
            
            Sleeping current thread is OK as this can't be called from
            the thread initializing this.
            
            Extra Note:
            
            On Windows the Timer Resolution means we can only sleep
            at minimum 15.6ms increments unless we request a lower one.
            
            So expect ~16ms sleeps there.
        */
#if DEBUG
        Debug.Assert(_initThreadId != Environment.CurrentManagedThreadId, "WaitUntilInitialized called from initialization thread");
#endif
        while (!_isInitialized)
            Thread.Sleep(8);
    }
}
