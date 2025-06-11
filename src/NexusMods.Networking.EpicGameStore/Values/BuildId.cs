using Reloaded.Memory.Extensions;

namespace NexusMods.Networking.EpicGameStore.Values;

/// <summary>
/// A unique identifier for a build in the Epic Game Store
/// </summary>
public readonly struct BuildId : IEquatable<BuildId>, IComparable<BuildId>
{
    private readonly string _value;

    public BuildId(string value)
    {
        _value = value;
    }

    public string Value => _value;

    public static BuildId FromUnsanitized(string unsanitized)
    {
        return From(unsanitized.ToLowerInvariantFast());
    }
    
    public static BuildId From(string value)
    {
        return new BuildId(value.ToLowerInvariantFast());
    }

    public bool Equals(BuildId other)
    {
        return string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);
    }

    public int CompareTo(BuildId other)
    {
        return string.Compare(_value, other._value, StringComparison.OrdinalIgnoreCase);
    }
}
