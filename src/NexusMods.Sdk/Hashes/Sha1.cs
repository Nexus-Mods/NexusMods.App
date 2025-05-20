using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusMods.Sdk.Hashes;

/// <summary>
/// A SHA-1 hash, 20 bytes long.
/// </summary>
[JsonConverter(typeof(Sha1JsonConverter))]
public unsafe struct Sha1 : IEquatable<Sha1>
{
    public bool Equals(Sha1 other)
    {
        return WritableSpan.SequenceEqual(other.WritableSpan);
    }

    public override bool Equals(object? obj)
    {
        return obj is Sha1 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return MemoryMarshal.Read<int>(WritableSpan);
    }

    private fixed byte _value[20];
    
    /// <summary>
    /// Get the hash as a byte span.
    /// </summary>
    public static Sha1 From(ReadOnlySpan<byte> value)
    {
        if (value.Length != 20)
            throw new ArgumentException("The value must be 20 bytes long.", nameof(value));
        
        var sha = new Sha1();
        value.CopyTo(sha.WritableSpan);
        
        return sha;
    }
    
    /// <summary>
    /// Convert the hash to a byte array.
    /// </summary>
    public byte[] ToArray()
    {
        var array = new byte[20];
        WritableSpan.CopyTo(array);
        return array;
    }

    /// <summary>
    /// Allocation free parsing of a SHA-1 hash from a hex string.
    /// </summary>
    public static Sha1 ParseFromHex(string hex)
    {
        if (hex.Length != 40)
            throw new ArgumentException("The hex string must be 40 characters long.", nameof(hex));
        
        var sha = new Sha1();
        Convert.FromHexString(hex, sha.WritableSpan, out _, out _);
        return sha;
    }

    /// <summary>
    /// Try to convert the hash to a hex string span.
    /// </summary>
    public bool TryToHex(Span<char> hex)
    {
        Debug.Assert(hex.Length >= 40);
        return Convert.TryToHexString(WritableSpan, hex, out _);
    }
    
    /// <summary>
    /// Convert the hash to a hex string.
    /// </summary>
    public override string ToString()
    {
        return Convert.ToHexString(WritableSpan);
    }

    /// <summary>
    /// Get a span to the value.
    /// </summary>
    private Span<byte> WritableSpan
    {
        get
        {
            fixed(byte* pValue = _value)
            {
                return new(pValue, 20);
            }
        }
    }
}

internal class Sha1JsonConverter : JsonConverter<Sha1>
{
    public override Sha1 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Sha1.ParseFromHex(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, Sha1 value, JsonSerializerOptions options)
    {
        Span<char> hex = stackalloc char[40];
        value.TryToHex(hex);
        writer.WriteStringValue(hex);
    }
}
