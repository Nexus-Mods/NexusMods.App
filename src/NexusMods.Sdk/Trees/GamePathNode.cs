using NexusMods.Paths;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;
using NexusMods.Sdk.Games;

namespace NexusMods.Sdk.Trees;

/// <summary>
///     Represents a tree node used for storing a game pathed location.
/// </summary>
/// <typeparam name="TValue">Type of value contained within the tree.</typeparam>
public struct GamePathNode<TValue> :
    IHaveBoxedChildrenWithKey<RelativePath, GamePathNode<TValue>>, IHaveValue<TValue>, IHavePathSegment,
    IHaveParent<GamePathNode<TValue>>, IHaveAFileOrDirectory, IEquatable<GamePathNode<TValue>>

{
    /// <inheritdoc />
    public Dictionary<RelativePath, KeyedBox<RelativePath, GamePathNode<TValue>>> Children { get; init; } // 0

    /// <inheritdoc />
    public RelativePath Segment { get; init; } // 8

    /// <inheritdoc />
    public Box<GamePathNode<TValue>>? Parent { get; init; } // 16

    /// <inheritdoc />
    public bool IsFile { get; init; } // 24 (size 1-8, depending on padding)

    /// <summary>
    ///     Hashcode.
    /// </summary>
    private int _hashCode; // 28 (size 4)
    
    /// <summary>
    ///     Contains the full path to the file.
    /// </summary>
    /// <remarks>
    ///     We sacrifice some memory here in order to speed up `Equals` and `GetHashCode`.
    /// </remarks>
    public GamePath GamePath { get; private set; } // 32

    /// <inheritdoc />
    public TValue Value { get; init; } // ?? (variable, potentially not 8 aligned)

    /// <summary>
    ///    The location ID of this node.
    /// </summary>
    public LocationId Id { get; init; } // ?? (variable, always 8 aligned)

    /// <summary>
    ///     Creates nodes from an IEnumerable of items.
    /// </summary>
    public static KeyedBox<RelativePath, GamePathNode<TValue>> Create(IEnumerable<KeyValuePair<GamePath, TValue>> items)
    {
        // Unboxed root node.
        var rootLocationId = items.FirstOrDefault().Key.LocationId!;
        var root = new KeyedBox<RelativePath, GamePathNode<TValue>>()
        {
            Item = new GamePathNode<TValue>
            {
                Children = new Dictionary<RelativePath, KeyedBox<RelativePath, GamePathNode<TValue>>>(),
                Segment = RelativePath.Empty,
                Parent = null,
                IsFile = false,
                Id = rootLocationId,
                Value = default(TValue)!,
                GamePath = new GamePath(rootLocationId, ""),
                _hashCode = 0,
            }
        };

        // Add each entry to the tree.
        foreach (var entry in items)
        {
            var path = entry.Key;
            var current = root;
            var parts = path.Path.GetParts();

            for (var x = 0; x < parts.Length; x++)
            {
                var segment = parts[x];
                var isFile = x == parts.Length - 1;

                // Try get existing child for this segment.
                if (!current.Item.Children.TryGetValue(segment, out var child))
                {
                    child = new KeyedBox<RelativePath, GamePathNode<TValue>>()
                    {
                        Item = new GamePathNode<TValue>
                        {
                            Children = new Dictionary<RelativePath, KeyedBox<RelativePath, GamePathNode<TValue>>>(),
                            Segment = segment,
                            Parent = current,
                            IsFile = isFile,
                            Id = entry.Key.LocationId,
                            Value = entry.Value,
                        }
                    };

                    var existingPath = current.GamePath().Path;
                    child.Item.GamePath = new GamePath(child.Item.Id, existingPath.Join(segment));
                    child.Item._hashCode = child.Item.MakeHashCode(); // depends on GamePath
                    current.Item.Children.Add(segment, child);
                }

                current = child;
            }
        }

        return root;
    }

    /// <summary>
    ///     Check for equality against other node.
    /// </summary>
    public bool Equals(GamePathNode<TValue> other) => GamePath.Equals(other.GamePath);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is GamePathNode<TValue> other && Equals(other);
    
    /// <inheritdoc />
    public override int GetHashCode() => _hashCode; // Cached to maximize performance.

    private int MakeHashCode() => GamePath.GetHashCode();
}

/// <summary>
///     Extension methods for <see cref="GamePathNode{TValue}" />.
/// </summary>
public static class GamePathNodeExtensions
{
    /// <summary>
    ///     Gets the <see cref="GamePath{T}"/> for the given boxed node.
    /// </summary>
    /// <param name="keyedBox">The KeyedBox containing a ModFileTree instance.</param>
    public static GamePath GamePath<T>(this KeyedBox<RelativePath, GamePathNode<T>> keyedBox) => keyedBox.Item.GamePath;
}
