using Reloaded.Memory.Extensions;

namespace NexusMods.Abstractions.EpicGameStore.Values;

/// <summary>
/// An id for an item in the Epic Game Store "item", which is a generic term for anything that can be purchased or downloaded, such as games, DLC, etc.
/// </summary>
public readonly struct ItemId : IEquatable<ItemId>, IComparable<ItemId>
{
    private readonly string _value;

    public ItemId(string value)
    {
        _value = value;
    }

    public string Value => _value;

    public static ItemId FromUnsanitized(string unsanitized)
    {
        return From(unsanitized.ToLowerInvariantFast());
    }
    
    public static ItemId From(string value)
    {
        return new ItemId(value.ToLowerInvariantFast());
    }

    public bool Equals(ItemId other)
    {
        return string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);
    }

    public int CompareTo(ItemId other)
    {
        return string.Compare(_value, other._value, StringComparison.OrdinalIgnoreCase);
    }
    
}
