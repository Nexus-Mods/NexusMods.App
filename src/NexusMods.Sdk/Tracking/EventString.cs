using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using JetBrains.Annotations;

namespace NexusMods.Sdk.Tracking;

[PublicAPI]
public readonly struct EventString : IEquatable<EventString>, IComparable<EventString>
{
    public readonly JsonEncodedText EncodedText;
    public string Value => EncodedText.Value;

    public EventString(string text)
    {
        EncodedText = JsonEncodedText.Encode(text);
    }

    public EventString(JsonEncodedText text)
    {
        EncodedText = text;
    }

    public static implicit operator EventString(string text) => new(text);
    public static implicit operator EventString(JsonEncodedText text) => new(text);

    public override string ToString() => EncodedText.Value;
    public bool Equals(EventString other) => Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
    public static bool operator ==(EventString left, EventString right) => left.Equals(right);
    public static bool operator !=(EventString left, EventString right) => !(left == right);
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is EventString other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode(StringComparison.OrdinalIgnoreCase);
    public int CompareTo(EventString other) => string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
}
