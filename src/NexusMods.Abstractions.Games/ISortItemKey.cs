namespace NexusMods.Abstractions.Games;

/// <summary>
/// Represents a game-specific identifier for a sortable item
/// </summary>
public interface ISortItemKey : IEquatable<ISortItemKey> { }

/// <summary>
/// Convenience container for a sortable item key
/// </summary>
public class SortItemKey<TKey> : ISortItemKey, IEquatable<SortItemKey<TKey>>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="key"></param>
    public SortItemKey(TKey key)
    {
        Key = key;
    }
    
    /// <summary>
    /// The inner identifier value
    /// </summary>
    public TKey Key { get; }

    /// <inheritdoc />
    public bool Equals(ISortItemKey? other)
    {
        if (other is SortItemKey<TKey> otherId)
        {
            return Key.Equals(otherId.Key);
        }

        return false;
    }

    public bool Equals(SortItemKey<TKey>? other)
    {
        if (other is null) return false;
        return other.Key.Equals(Key);
    }

    /// <inheritdoc cref="GetHashCode" />
    public override int GetHashCode()
    {
        return Key.GetHashCode();
    }
}
