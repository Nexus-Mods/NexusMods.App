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

    /// <summary>
    /// Game registry for all installed games.
    /// </summary>
    public Registry(IEnumerable<ILocatableGame> games, IConnection connection)
    {
        _connection = connection;
        _startup = Task.Run(() => Startup(games));
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
        foreach (var install in allInstalls)
        {
            if (allInDb.TryGetValue((install.Game.Domain, install.Store), out var found))
            {
                install.Id = found.Id;
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
                install.Id = result[id];
        }

        _byId = added.ToDictionary(x => x.Item2.Id, x => x.Item2);
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
}
