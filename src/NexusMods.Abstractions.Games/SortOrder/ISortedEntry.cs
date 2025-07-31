namespace NexusMods.Abstractions.Games;

/// <summary>
/// Represents an entry in a stored sort order.
/// </summary>
public interface ISortedEntry
{
    public ISortItemKey Key { get;}
    
    public int SortIndex { get; set; }
}

public interface ISortedEntry<out TKey> : ISortedEntry
    where TKey : IEquatable<TKey>, ISortItemKey
{
    /// <inheritdoc />
    ISortItemKey ISortedEntry.Key => Key;
    
    /// <summary>
    /// <inheritdoc cref="ISortableItemLoadoutData.Key"/>
    /// Generic version of the key property
    /// </summary>
    new TKey Key { get; }
}

/// <summary>
/// Default implementation of <see cref="ISortedEntry{TKey}"/>.
/// This class is used to represent an entry in a stored sort order.
/// </summary>
/// <typeparam name="TKey"></typeparam>
public class SortedEntry<TKey> : ISortedEntry<TKey>
    where TKey : IEquatable<TKey>, ISortItemKey
{
    public SortedEntry(TKey key, int sortIndex)
    {
        Key = key;
        SortIndex = sortIndex;
    }

    public TKey Key { get; }
    
    public int SortIndex { get; set; }
}
