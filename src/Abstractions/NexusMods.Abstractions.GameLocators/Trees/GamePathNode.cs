using System.Runtime.CompilerServices;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Paths;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.Abstractions.Games.Trees;

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

    /// <inheritdoc />
    public TValue Value { get; init; } // ?? (variable, potentially not 8 aligned)

    /// <summary>
    ///    The location ID of this node.
    /// </summary>
    public LocationId Id { get; init; } // ?? (variable, always 8 aligned)

    /// <summary>
    ///     Retrieves the corresponding <see cref="GamePath" /> from given location ID and this node's path.
    /// </summary>
    public GamePath GetGamePath() => new(Id, this.ReconstructPath());

    /// <summary>
    ///     Creates nodes from an IEnumerable of items.
    /// </summary>
    public static KeyedBox<RelativePath, GamePathNode<TValue>> Create(IEnumerable<KeyValuePair<GamePath, TValue>> items)
    {
        // Unboxed root node.
        var root = new KeyedBox<RelativePath, GamePathNode<TValue>>()
        {
            Item = new GamePathNode<TValue>
            {
                Children = new Dictionary<RelativePath, KeyedBox<RelativePath, GamePathNode<TValue>>>(),
                Segment = RelativePath.Empty,
                Parent = null,
                IsFile = false,
                Id = items.FirstOrDefault().Key.LocationId!,
                Value = default!,
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
                    child.Item._hashCode = child.Item.MakeHashCode();

                    current.Item.Children.Add(segment, child);
                }

                current = child;
            }
        }

        return root;
    }

    /// <summary>
    ///     Fast check for equality against other node.
    /// </summary>
    public bool Equals(GamePathNode<TValue> other)
    {
        // Start with comparing current nodes
        if (!Segment.Equals(other.Segment)) return false;
    
        // Traverse up the parent chain of both nodes, comparing segments
        var currentNodeParent = this.Parent;
        var otherNodeParent = other.Parent;
    
        while (currentNodeParent != null && otherNodeParent != null)
        {
            // If both parents are not null but their segments don't match, return false
            if (!currentNodeParent.Item.Segment.Equals(otherNodeParent.Item.Segment))
            {
                return false;
            }
        
            // Move up the tree
            currentNodeParent = currentNodeParent.Item.Parent;
            otherNodeParent = otherNodeParent.Item.Parent;
        }
    
        // If both currentNodeParent and otherNodeParent are null, we reached the root and all segments matched
        // If only one is null, the trees have different depths and are not equal
        return currentNodeParent == null && otherNodeParent == null;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is GamePathNode<TValue> other && Equals(other);
    
    /// <inheritdoc />
    public override int GetHashCode() => _hashCode; // Cached to maximize performance.

    private int MakeHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Segment);

        var parent = this.Parent;
        while (parent != null)
        {
            hashCode.Add(parent.Segment());
            parent = parent.Parent();
        }
        
        return hashCode.ToHashCode();
    }
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
    public static GamePath GamePath<T>(this KeyedBox<RelativePath, GamePathNode<T>> keyedBox) => keyedBox.Item.GetGamePath();
}
