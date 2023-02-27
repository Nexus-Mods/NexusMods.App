using NexusMods.Paths.Extensions;

namespace NexusMods.Paths;

/// <summary>
/// Stores the path for an individual game.
/// </summary>
public struct GamePath : IPath, IEquatable<GamePath>
{
    /// <summary>
    /// The path to this instance.
    /// </summary>
    public RelativePath Path { get; } = RelativePath.Empty;
    
    /// <summary>
    /// Type of folder stored in this instance.
    /// </summary>
    public GameFolderType Type { get; }
    
    /// <inheritdoc />
    public Extension Extension => Path.Extension;

    /// <inheritdoc />
    public RelativePath FileName => Path.FileName;

    /// <summary/>
    /// <param name="type">Type of folder contained in this path.</param>
    /// <param name="path">The path to the item.</param>
    public GamePath(GameFolderType type, RelativePath path)
    {
        Type = type;
        Path = path;
    }
    
    /// <summary/>
    /// <param name="type">Type of folder contained in this path.</param>
    /// <param name="path">The path to the item.</param>
    public GamePath(GameFolderType type, string path) : this(type, path.ToRelativePath()) { }

    /// <inheritdoc />
    public bool Equals(GamePath other) => Type == other.Type && Path == other.Path;

    /// <summary/>
    public static bool operator ==(GamePath a, GamePath b) => a.Type == b.Type && a.Path == b.Path;

    /// <summary/>
    public static bool operator !=(GamePath a, GamePath b) => a.Type != b.Type || a.Path != b.Path;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is GamePath other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Path.GetHashCode() ^ (int)Type;

    /// <inheritdoc />
    public override string ToString() => "{" + Type + "}\\" + Path;

    /// <summary>
    /// Joins the current absolute path with a relative path.
    /// </summary>
    /// <param name="folderPath">
    ///    The absolute path to combine with current relative path.
    /// </param>
    public AbsolutePath CombineChecked(AbsolutePath folderPath) => folderPath.CombineChecked(Path);
}

/// <summary>
///     The base folder for the GamePath, more values can easily be added here as needed
/// </summary>
public enum GameFolderType : byte
{
    /// <summary>
    /// Stores the path for the game.
    /// </summary>
    Game = 0,
    
    /// <summary>
    /// Path used to store the save data of a game.
    /// </summary>
    Saves,
    
    /// <summary>
    /// Path used to store player settings/preferences.
    /// </summary>
    Preferences,
    
    /// <summary>
    /// Stores other application data [sometimes including save data].
    /// </summary>
    AppData
}