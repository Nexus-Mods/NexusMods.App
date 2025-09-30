namespace NexusMods.Abstractions.Games;

/// <summary>
/// Represents an entry in a stored sort order.
/// </summary>
public interface ISortItemData : IComparable<ISortItemData>
{
    public ISortItemKey Key { get;}
    
    public int SortIndex { get; set; }
}

public interface ISortItemData<out TKey> : ISortItemData
    where TKey : IEquatable<TKey>, ISortItemKey
{
    /// <inheritdoc />
    ISortItemKey ISortItemData.Key => Key;
    
    /// <summary>
    /// <inheritdoc cref="ISortItemLoadoutData.Key"/>
    /// Generic version of the key property
    /// </summary>
    new TKey Key { get; }
}

/// <summary>
/// Default implementation of <see cref="ISortItemData{TKey}"/>.
/// This class is used to represent an entry in a stored sort order.
/// </summary>
/// <typeparam name="TKey"></typeparam>
public class SortItemData<TKey> : ISortItemData<TKey>, IComparable<SortItemData<TKey>> 
    where TKey : IEquatable<TKey>, ISortItemKey
{
    public SortItemData(TKey key, int sortIndex)
    {
        Key = key;
        SortIndex = sortIndex;
    }

    public TKey Key { get; }
    
    public int SortIndex { get; set; }

    public int CompareTo(SortItemData<TKey>? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        return SortIndex.CompareTo(other.SortIndex);
    }

    public int CompareTo(ISortItemData? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        return SortIndex.CompareTo(other.SortIndex);
    }
}
