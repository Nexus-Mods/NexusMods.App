using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// A game locator that allows for games identified by the user and added manually.
/// </summary>
public class ManuallyAddedLocator : IGameLocator
{
    private readonly Lazy<IConnection> _conn;
    private readonly IFileSystem _fileSystem;
    private readonly IServiceProvider _provider;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="provider"></param>
    public ManuallyAddedLocator(IServiceProvider provider, IFileSystem fileSystem)
    {
        _provider = provider;
        _fileSystem = fileSystem;
        _conn = new Lazy<IConnection>(provider.GetRequiredService<IConnection>);
    }

    /// <summary>
    /// Adds a manually added game to the store.
    /// </summary>
    /// <param name="game"></param>
    /// <param name="version"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public async Task<(EntityId, GameInstallation)> Add(IGame game, Version version, AbsolutePath path)
    {
        using var tx = _conn.Value.BeginTransaction();
        var ent = new ManuallyAddedGame.New(tx)
        {
            GameId = game.NexusModsGameId,
            Version = version.ToString(),
            Path = path.ToString(),
        };
        var result = await tx.Commit();
        var addedGame = result.Remap(ent);
        var gameRegistry = _provider.GetRequiredService<IGameRegistry>();
        var install = await gameRegistry.Register(game, new GameLocatorResult(path, path.FileSystem, OSInformation.Shared, GameStore.ManuallyAdded, addedGame, version), this);
        var newId = result[ent.Id];
        return (newId, install);
    }

    /// <summary>
    /// Removes a manually added game from the store. Verifies that the id is a valid 'ManuallyAddedGame' id.
    /// </summary>
    public async Task Remove(EntityId id)
    {
        var ent = ManuallyAddedGame.Load(_conn.Value.Db, id);
        if (!ent.Contains(ManuallyAddedGame.GameId))
            throw new ArgumentOutOfRangeException(nameof(id), "The id must be a valid 'ManuallyAddedGame'");

        using var tx = _conn.Value.BeginTransaction();

        tx.Delete(id, false);
        
        await tx.Commit();
    }

    /// <inheritdoc />
    public IEnumerable<GameLocatorResult> Find(ILocatableGame game, bool forceRefreshCache = false)
    {
        var games = ManuallyAddedGame.FindByGameId(_conn.Value.Db, game.NexusModsGameId)
            .Select(g => new GameLocatorResult(_fileSystem.FromUnsanitizedFullPath(g.Path), _fileSystem,
                OSInformation.Shared, GameStore.ManuallyAdded, g, Version.Parse(g.Version)));
        return games;
    }
}
