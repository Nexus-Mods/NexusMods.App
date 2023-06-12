using System.Collections.ObjectModel;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.Paths;

namespace NexusMods.DataModel.Games.ManuallyAdded;

public class GameStore : IGameLocator
{
    private readonly IDataStore _store;

    public GameStore(IDataStore store, IEnumerable<IGame> games)
    {
        _store = store;
        _games = games.ToArray();
    }

    public void Add(IGame game, Version version, AbsolutePath path)
    {
        _store.Put(new ManuallyAddedGame
        {
            GameDomain = game.Domain,
            Version = version,
            Path = path
        });
    }
~
    public IEnumerable<GameLocatorResult> Find(IGame game)
    {
        var allGames = _store.GetByPrefix<ManuallyAddedGame>(new Id64(EntityCategory.ManuallyAddedGame, 0));
        var games = allGames.Where(g => g.GameDomain == game.Domain);
        return games.Select(g => new GameLocatorResult(g.Path, GameStore.ManuallyAdded, g, g.Version));
    }
}
