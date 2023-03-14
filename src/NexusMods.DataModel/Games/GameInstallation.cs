using NexusMods.Paths;

namespace NexusMods.DataModel.Games;

/// <summary>
/// Defines an individual installation of a game, i.e. a unique combination of
/// Version and Location.
/// </summary>
public class GameInstallation
{
    /// <summary>
    /// The Version installed.
    /// </summary>
    public Version Version { get; init; } = new();

    /// <summary>
    /// The location on-disk of this game and it's associated paths [e.g. Saves].
    /// </summary>
    public IReadOnlyDictionary<GameFolderType, AbsolutePath> Locations { get; init; } =
        new Dictionary<GameFolderType, AbsolutePath>();

    /// <summary>
    /// The game to which this installation belongs.
    /// </summary>
    public IGame Game { get; init; } = null!;

    /// <summary>
    /// Empty game installation, used for testing and some cases where a property must be set.
    /// </summary>
    public static GameInstallation Empty => new();

    /// <summary>
    /// Returns the game name and version as
    /// </summary>
    /// <returns></returns>
    public override string ToString() => $"{Game.Name} v{Version}";

    /// <summary>
    /// Converts a <see cref="AbsolutePath"/> to a <see cref="GamePath"/> assuming the path exists under a game path.
    /// </summary>
    /// <param name="path">The path to convert.</param>
    /// <returns>Path to the game.</returns>
    public GamePath ToGamePath(AbsolutePath path)
    {
        return Locations.Where(l => path.InFolder(l.Value))
            .Select(l => new GamePath(l.Key, path.RelativeTo(l.Value)))
            .MinBy(x => x.Path.Depth);
    }

    /// <summary>
    /// Utility method used to determine whether <see cref="Game"/> can
    /// be casted to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to cast the game to.</typeparam>
    /// <returns>True if the cast is possible, else false.</returns>
    public bool Is<T>() where T : IGame => Game is T;
}
