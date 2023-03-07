using NexusMods.Paths;

namespace NexusMods.DataModel.Games;

/// <summary>
/// Description of a given installation of a given name
/// </summary>
public class GameInstallation
{
    /// <summary>
    /// The Version installed
    /// </summary>
    public Version Version { get; init; } = new();

    /// <summary>
    /// The location on-disk of this game and it's associated paths
    /// </summary>
    public IReadOnlyDictionary<GameFolderType, AbsolutePath> Locations { get; init; } =
        new Dictionary<GameFolderType, AbsolutePath>();

    /// <summary>
    /// The game to which this installation belongs
    /// </summary>
    public IGame Game { get; init; } = null!;

    /// <summary>
    /// Empty game installation, used for testing and some cases where a property must be set
    /// </summary>
    public static GameInstallation Empty => new();


    /// <summary>
    /// Returns the game name and version as 
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"{Game.Name} v{Version}";
    }

    /// <summary>
    /// Converts a Absolute path to a relative path assuming the path exists under a game path. 
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public GamePath ToGamePath(AbsolutePath path)
    {
        return Locations.Where(l => path.InFolder(l.Value))
            .Select(l => new GamePath(l.Key, path.RelativeTo(l.Value)))
            .MinBy(x => x.Path.Depth);
    }

    public bool Is<T>() where T : IGame => Game is T;
}
