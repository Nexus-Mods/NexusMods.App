using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.DataModel.GameRegistry;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel;

/// <summary>
/// Game registry for all installed games.
/// </summary>
public class Registry : IGameRegistry
{
    private readonly IConnection _connection;
    private readonly Task _startup;
    private Dictionary<EntityId,GameInstallation> _byId = new();
    private Dictionary<(GameDomain Domain, Version Version, GameStore Store),EntityId> _byInstall = new();

    private readonly SourceCache<(GameInstallation Game, EntityId Id), EntityId> _cache = new(x => x.Id); 
    
    private readonly ReadOnlyObservableCollection<GameInstallation> _installedGames;

    /// <inheritdoc />
    public ReadOnlyObservableCollection<GameInstallation> InstalledGames => _installedGames;

    /// <summary>
    /// Game registry for all installed games.
    /// </summary>
    public Registry(IEnumerable<ILocatableGame> games, IConnection connection)
    {
        _connection = connection;
        _startup = Task.Run(() => Startup(games));
        
        _cache
            .Connect()
            .Transform(g => g.Game)
            .Bind(out _installedGames)
            .Subscribe();
    }

    private async Task Startup(IEnumerable<ILocatableGame> games)
    {
        var allInstalls = from game in games
            let igame = (IGame)game
            from install in igame.Installations
            select install;

        var allInDb = GameMetadata
            .All(_connection.Db)
            .ToDictionary(x => (GameDomain.From(x.Domain), GameStore.From(x.Store)));

        using var tx = _connection.BeginTransaction();

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
            var result = await tx.Commit();
            foreach (var (id, install) in added)
                results.Add((result[id], install));
        }

        _byId = results.ToDictionary(x => x.Id, x => x.Item2);
        _byInstall = results.ToDictionary(x => GetKey(x.Item2), x => x.Id);
        
        _cache.Edit(x => {
            x.Clear();
            foreach (var (id, install) in results)
                _cache.AddOrUpdate((install, id));
        });
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
            if (!_startup.IsCompleted)
                _startup.Wait();
            return _byId.Values;
        }
    }

    /// <inheritdoc />
    public GameInstallation Get(EntityId id)
    {
        if (!_startup.IsCompleted)
            _startup.Wait();
        return _byId[id];
    }

    /// <inheritdoc />
    public EntityId GetId(GameInstallation installation)
    {
        if (!_startup.IsCompleted)
            _startup.Wait();
        return _byInstall[GetKey(installation)];
    }
}
