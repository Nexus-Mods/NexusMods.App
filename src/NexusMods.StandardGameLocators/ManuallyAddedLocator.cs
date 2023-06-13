using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators;

public class ManuallyAddedLocator : IGameLocator
{
    private readonly Lazy<IDataStore> _store;

    public ManuallyAddedLocator(IServiceProvider provider)
    {
        _store = new Lazy<IDataStore>(provider.GetRequiredService<IDataStore>);
    }

    public IId Add(IGame game, Version version, AbsolutePath path)
    {
        return _store.Value.Put(new ManuallyAddedGame
        {
            GameDomain = game.Domain,
            Version = version,
            Path = path
        });
    }

    public void Remove(IId id)
    {
        if (id.Category != EntityCategory.ManuallyAddedGame)
            throw new ArgumentOutOfRangeException(nameof(id), "The id must be a valid 'ManuallyAddedGame'");
        
        _store.Value.Delete(id);
    }
    
    public IEnumerable<GameLocatorResult> Find(IGame game)
    {
        var allGames = _store.Value.GetByPrefix<ManuallyAddedGame>(new IdVariableLength(EntityCategory.ManuallyAddedGame, Array.Empty<byte>()));
        var games = allGames.Where(g => g.GameDomain == game.Domain);
        return games.Select(g => new GameLocatorResult(g.Path, GameStore.ManuallyAdded, g, g.Version));
    }
}
