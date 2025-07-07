using Reloaded.Memory.Extensions;

namespace NexusMods.Abstractions.EpicGameStore.Values;

public readonly struct ManifestHash : IEquatable<ManifestHash>, IComparable<ManifestHash>
{
    private readonly string _value;

    public ManifestHash(string value)
    {
        _value = value;
    }
    
    public override string ToString()
    {
        return _value;
    }

    public string Value => _value;

    public static ManifestHash FromUnsanitized(string unsanitized)
    {
        return From(unsanitized.ToLowerInvariantFast());
    }
    
    public static ManifestHash From(string value)
    {
        return new ManifestHash(value.ToLowerInvariantFast());
    }

    public bool Equals(ManifestHash other)
    {
        return string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);
    }

    public int CompareTo(ManifestHash other)
    {
        return string.Compare(_value, other._value, StringComparison.OrdinalIgnoreCase);
    }
}
