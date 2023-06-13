using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// A game locator that allows for games identified by the user and added manually.
/// </summary>
public class ManuallyAddedLocator : IGameLocator
{
    private readonly Lazy<IDataStore> _store;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="provider"></param>
    public ManuallyAddedLocator(IServiceProvider provider)
    {
        _store = new Lazy<IDataStore>(provider.GetRequiredService<IDataStore>);
    }

    /// <summary>
    /// Adds a manually added game to the store.
    /// </summary>
    /// <param name="game"></param>
    /// <param name="version"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public IId Add(IGame game, Version version, AbsolutePath path)
    {
        return _store.Value.Put(new ManuallyAddedGame
        {
            GameDomain = game.Domain,
            Version = version,
            Path = path
        });
    }

    /// <summary>
    /// Removes a manually added game from the store. Verifies that the id is a valid 'ManuallyAddedGame' id.
    /// </summary>
    /// <param name="id"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Remove(IId id)
    {
        if (id.Category != EntityCategory.ManuallyAddedGame)
            throw new ArgumentOutOfRangeException(nameof(id), "The id must be a valid 'ManuallyAddedGame'");
        
        _store.Value.Delete(id);
    }
    
    /// <inheritdoc />
    public IEnumerable<GameLocatorResult> Find(IGame game)
    {
        var allGames = _store.Value.GetByPrefix<ManuallyAddedGame>(new IdVariableLength(EntityCategory.ManuallyAddedGame, Array.Empty<byte>()));
        var games = allGames.Where(g => g.GameDomain == game.Domain);
        return games.Select(g => new GameLocatorResult(g.Path, GameStore.ManuallyAdded, g, g.Version));
    }
}
