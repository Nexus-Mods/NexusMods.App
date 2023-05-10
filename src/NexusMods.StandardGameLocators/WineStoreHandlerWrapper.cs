using GameFinder.Common;
using GameFinder.RegistryUtils;
using GameFinder.Wine;
using NexusMods.DataModel.Games;
using NexusMods.Paths;
using OneOf;
using IGame = GameFinder.Common.IGame;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Helper class that uses all registered store handlers to find games inside wine prefixes.
/// </summary>
public class WineStoreHandlerWrapper
{
    /// <summary>
    /// Delegate for creating a <see cref="AHandler"/> given a <see cref="AWinePrefix"/>, an implementation
    /// of <see cref="IRegistry"/> and an implementation of <see cref="IFileSystem"/>. The registry and
    /// file system are overlays and created from the wine prefix.
    /// </summary>
    public delegate AHandler CreateHandler(AWinePrefix winePrefix, IRegistry wineRegistry, IFileSystem wineFileSystem);

    /// <summary>
    /// Delegate for trying to match the requested game with a found game.
    /// </summary>
    public delegate GameLocatorResult? Matches(IGame foundGame, DataModel.Games.IGame requestedGame);

    private readonly IFileSystem _fileSystem;
    private readonly CreateHandler[] _factories;
    private readonly Matches[] _matchers;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="fileSystem"></param>
    /// <param name="factories"></param>
    /// <param name="matchers"></param>
    public WineStoreHandlerWrapper(IFileSystem fileSystem,
        CreateHandler[] factories,
        Matches[] matchers)
    {
        _fileSystem = fileSystem;
        _factories = factories;
        _matchers = matchers;
    }

    /// <summary>
    /// Tries to match the requested game with one of the found games and returns
    /// a non-null <see cref="GameLocatorResult"/> on success, else <c>null</c>.
    /// </summary>
    /// <param name="foundGames"></param>
    /// <param name="requestedGame"></param>
    /// <returns></returns>
    public GameLocatorResult? FindMatchingGame(IEnumerable<IGame> foundGames, DataModel.Games.IGame requestedGame)
    {
        foreach (var foundGame in foundGames)
        {
            foreach (var matcher in _matchers)
            {
                var res = matcher(foundGame, requestedGame);
                if (res is not null) return res;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds all games inside the given prefix using the registered store handlers.
    /// </summary>
    /// <param name="winePrefix"></param>
    /// <returns></returns>
    public IEnumerable<OneOf<IGame, ErrorMessage>> FindAllGamesInPrefix(AWinePrefix winePrefix)
    {
        var wineRegistry = winePrefix.CreateRegistry(_fileSystem);
        var wineFileSystem = winePrefix.CreateOverlayFileSystem(_fileSystem);

        foreach (var factory in _factories)
        {
            var handler = factory(winePrefix, wineRegistry, wineFileSystem);

            foreach (var res in handler.FindAllInterfaceGames())
            {
                yield return res;
            }
        }
    }
}
