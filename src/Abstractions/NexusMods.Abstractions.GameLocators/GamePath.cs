using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// Stores the path for an individual game.
/// </summary>
public readonly struct GamePath : IPath<GamePath>, IEquatable<GamePath>
{
    /// <summary>
    /// The path to this instance.
    /// </summary>
    public RelativePath Path { get; } = RelativePath.Empty;

    /// <summary>
    /// The id of the game location this path is relative to.
    /// </summary>
    public LocationId LocationId { get; }

    /// <inheritdoc />
    public Extension Extension => Path.Extension;

    /// <inheritdoc />
    public RelativePath FileName => Path.FileName;

    /// <summary/>
    /// <param name="locationId">Id of the game location to be relative to.</param>
    /// <param name="path">The relative path to the item.</param>
    public GamePath(LocationId locationId, RelativePath path)
    {
        LocationId = locationId;
        Path = path;
    }

    /// <summary/>
    /// <param name="locationId">Id of the game location to be relative to.</param>
    /// <param name="path">The relative path to the item.</param>
    public GamePath(LocationId locationId, string path) : this(locationId, path.ToRelativePath()) { }

    /// <inheritdoc />
    public bool Equals(GamePath other) => LocationId == other.LocationId && Path == other.Path;

    /// <summary/>
    public static bool operator ==(GamePath a, GamePath b) => a.LocationId == b.LocationId && a.Path == b.Path;

    /// <summary/>
    public static bool operator !=(GamePath a, GamePath b) => a.LocationId != b.LocationId || a.Path != b.Path;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is GamePath other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Path.GetHashCode() ^ LocationId.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => "{" + LocationId + "}/" + Path;

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
    public GamePath Parent => new GamePath(LocationId, Path.Parent);

    /// <inheritdoc />
    public GamePath GetRootComponent => new GamePath(LocationId, "");

    /// <inheritdoc />
    public IEnumerable<RelativePath> Parts => Path.Parts;

    /// <inheritdoc />
    public IEnumerable<GamePath> GetAllParents()
    {
        var id = LocationId;
        var root = new GamePath(id, "");
        return Path.GetAllParents().Select(parentPath => new GamePath(id, parentPath)).Append(root);
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
        return LocationId == parent.LocationId && Path.InFolder(parent.Path);
    }

    /// <inheritdoc />
    public bool StartsWith(GamePath other)
    {
        return LocationId == other.LocationId && Path.StartsWith(other.Path);
    }

    /// <inheritdoc />
    public bool EndsWith(RelativePath other)
    {
        return Path.EndsWith(other);
    }
}
