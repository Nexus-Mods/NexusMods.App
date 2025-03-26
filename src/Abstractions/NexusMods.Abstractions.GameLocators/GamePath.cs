using System.Diagnostics;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// Stores the path for an individual game.
/// </summary>
public readonly struct GamePath : IPath<GamePath>, IEquatable<GamePath>, IComparable<GamePath>
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
    
    /// <summary>
    /// Implicitly converts a tuple of a LocationId and a RelativePath to a GamePath.
    /// </summary>
    public static implicit operator GamePath((LocationId LocationId, RelativePath Path) value) => new(value.LocationId, value.Path);

    /// <summary/>
    /// <param name="locationId">Id of the game location to be relative to.</param>
    /// <param name="path">The relative path to the item.</param>
    public GamePath(LocationId locationId, string path) : this(locationId, RelativePath.FromUnsanitizedInput(path)) { }

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

    /// <inheritdoc />
    public int CompareTo(GamePath other)
    {
        var locationComparison = LocationId.CompareTo(other.LocationId);
        return locationComparison != 0 ? locationComparison : Path.CompareTo(other.Path);
    }
    
    /// <summary>
    /// Implicitly converts a tuple (such as from a GamePathParentAttribute) to a GamePath.
    /// </summary>
    public static implicit operator GamePath((EntityId, LocationId, RelativePath) value) => new(value.Item2, value.Item3);
    
    /// <summary>
    /// Converts a GamePath to a tuple (such as for a GamePathParentAttribute), using the provided EntityId, this id
    /// is allowed to be a temporary id and will be replaced with the actual id the value is transacted
    /// </summary>
    public (EntityId, LocationId, RelativePath) ToGamePathParentTuple(EntityId id) => (id, LocationId, Path);
}

/// <summary>
/// Defines a GamePath attribute.
/// </summary>
public class GamePathAttribute(string ns, string name) : ScalarAttribute<GamePath, string, Utf8Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override string ToLowLevel(GamePath value)
    {
        // TODO: make this a reference or something
        return $"{value.LocationId.Value}|{value.Path}";
    }

    /// <inheritdoc />
    protected override GamePath FromLowLevel(string value, AttributeResolver resolver)
    {
        var parts = value.Split('|');
        Debug.Assert(parts.Length == 2);
        return new GamePath(LocationId.From(parts[0]), RelativePath.FromUnsanitizedInput(parts[1]));
    }
}

/// <summary>
/// An attribute that combines an EntityId, LocationId and RelativePath into a single attribute. This is used to represent GamePaths prefixed
/// with a parent entity so that range queries only return the paths that are children of the parent entity.
/// </summary>
public class GamePathParentAttribute(string ns, string name) : ScalarAttribute<(EntityId, LocationId, RelativePath), (EntityId, ushort, string), Tuple3_Ref_UShort_Utf8I_Serializer>(ns, name) 
{
    /// <inheritdoc />
    protected override (EntityId, ushort, string) ToLowLevel((EntityId, LocationId, RelativePath) value) 
        => (value.Item1, value.Item2.Value, value.Item3);

    /// <inheritdoc />
    protected override (EntityId, LocationId, RelativePath) FromLowLevel((EntityId, ushort, string) value, AttributeResolver resolver) 
        => (value.Item1, LocationId.From(value.Item2), RelativePath.FromUnsanitizedInput(value.Item3));
}
