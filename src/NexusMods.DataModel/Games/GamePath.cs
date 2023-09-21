using System;
using NexusMods.Paths.Extensions;

namespace NexusMods.Paths;

/// <summary>
/// Stores the path for an individual game.
/// </summary>
public readonly struct GamePath : IPath, IPath<GamePath>, IEquatable<GamePath>
{
    /// <summary>
    /// The path to this instance.
    /// </summary>
    public RelativePath Path { get; } = RelativePath.Empty;

    /// <summary>
    /// Type of folder stored in this instance.
    /// </summary>
    public LocationId Type { get; }

    /// <inheritdoc />
    public Extension Extension => Path.Extension;

    /// <inheritdoc />
    public RelativePath FileName => Path.FileName;

    /// <summary/>
    /// <param name="type">Type of folder contained in this path.</param>
    /// <param name="path">The path to the item.</param>
    public GamePath(LocationId type, RelativePath path)
    {
        Type = type;
        Path = path;
    }

    /// <summary/>
    /// <param name="type">Type of folder contained in this path.</param>
    /// <param name="path">The path to the item.</param>
    public GamePath(LocationId type, string path) : this(type, path.ToRelativePath()) { }

    /// <inheritdoc />
    public bool Equals(GamePath other) => Type == other.Type && Path == other.Path;

    /// <summary/>
    public static bool operator ==(GamePath a, GamePath b) => a.Type == b.Type && a.Path == b.Path;

    /// <summary/>
    public static bool operator !=(GamePath a, GamePath b) => a.Type != b.Type || a.Path != b.Path;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is GamePath other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Path.GetHashCode() ^ Type.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => "{" + Type + "}/" + Path;

    /// <summary>
    /// Joins the current absolute path with a relative path.
    /// </summary>
    /// <param name="folderPath">
    ///    The absolute path to combine with current relative path.
    /// </param>
    public AbsolutePath Combine(AbsolutePath folderPath) => folderPath.Combine(Path);

    /// <inheritdoc />
    public RelativePath Name => Path.Name;

    /// <inheritdoc />
    public GamePath Parent => new GamePath(Type, Path.Parent);

    /// <inheritdoc />
    public GamePath GetRootComponent => new GamePath(Type, "");

    /// <inheritdoc />
    public IEnumerable<RelativePath> Parts => Path.Parts;

    /// <inheritdoc />
    public IEnumerable<GamePath> GetAllParents()
    {
        var type = Type;
        var root = new GamePath(type, "");
        return Path.GetAllParents().Select(parentPath => new GamePath(type, parentPath)).Append(root);
    }

    /// <inheritdoc />
    public RelativePath GetNonRootPart()
    {
        return Path;
    }

    /// <inheritdoc />
    public bool IsRooted => true;

    /// <inheritdoc />
    public bool InFolder(GamePath parent)
    {
        return Type == parent.Type && Path.InFolder(parent.Path);
    }

    /// <inheritdoc />
    public bool StartsWith(GamePath other)
    {
        return Type == other.Type && Path.StartsWith(other.Path);
    }

    /// <inheritdoc />
    public bool EndsWith(RelativePath other)
    {
        return Path.EndsWith(other);
    }
}
