using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// A game locator that allows for games identified by the user and added manually.
/// </summary>
public class ManuallyAddedLocator : IGameLocator
{
    private readonly Lazy<IConnection> _store;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="provider"></param>
    public ManuallyAddedLocator(IServiceProvider provider, IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        _store = new Lazy<IConnection>(provider.GetRequiredService<IConnection>);
    }

    /// <summary>
    /// Adds a manually added game to the store.
    /// </summary>
    /// <param name="game"></param>
    /// <param name="version"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public async Task<EntityId> Add(IGame game, Version version, AbsolutePath path)
    {
        using var tx = _store.Value.BeginTransaction();
        var ent = new ManuallyAddedGame.Model(tx)
        {
            GameDomain = game.Domain,
            Version = version.ToString(),
            Path = path.ToString()
        };
        var result = await tx.Commit();
        return result[ent.Id];
    }

    /// <summary>
    /// Removes a manually added game from the store. Verifies that the id is a valid 'ManuallyAddedGame' id.
    /// </summary>
    public async Task Remove(EntityId id)
    {
        var ent = _store.Value.Db.Get<ManuallyAddedGame.Model>(id);
        if (!ent.Contains(ManuallyAddedGame.GameDomain))
            throw new ArgumentOutOfRangeException(nameof(id), "The id must be a valid 'ManuallyAddedGame'");

        using var tx = _store.Value.BeginTransaction();

        ManuallyAddedGame.GameDomain.Retract(ent);
        ManuallyAddedGame.Path.Retract(ent);
        ManuallyAddedGame.Version.Retract(ent);
        
        await tx.Commit();
    }

    /// <inheritdoc />
    public IEnumerable<GameLocatorResult> Find(ILocatableGame game)
    {
        var db = _store.Value.Db;
        var allGames = db.Find(ManuallyAddedGame.GameDomain)
            .Select(g => db.Get<ManuallyAddedGame.Model>(g));
        var games = allGames.Where(g => g.GameDomain == game.Domain);
        return games.Select(g => new GameLocatorResult(_fileSystem.FromUnsanitizedFullPath(g.Path), 
            GameStore.ManuallyAdded, g, Version.Parse(g.Version)));
    }
}
