namespace NexusMods.Abstractions.Games;

/// <summary>
/// Represents a game-specific identifier for a sortable item
/// </summary>
public interface ISortItemKey : IEquatable<ISortItemKey>
{
    /// <summary>
    /// The hash code computed on the actual key value
    /// </summary>
    int GetHashCode();
}

/// <summary>
/// Convenience container for a sortable item key
/// </summary>
public class SortItemKey<TKey> : ISortItemKey
    where TKey : notnull, IEquatable<TKey>
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

    /// <inheritdoc cref="GetHashCode" />
    public override int GetHashCode()
    {
        return Key.GetHashCode();
    }
}
