using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusMods.Abstractions.Hashes;

/// <summary>
/// A MD5 hash, 16 bytes long.
/// </summary>
[JsonConverter(typeof(Md5JsonConverter))]
public unsafe struct Md5 : IEquatable<Md5>
{
    public bool Equals(Md5 other)
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

    private fixed byte _value[16];
    
    /// <summary>
    /// Get the hash as a byte span.
    /// </summary>
    public static Md5 From(ReadOnlySpan<byte> value)
    {
        if (value.Length != 16)
            throw new ArgumentException("The value must be 16 bytes long.", nameof(value));
        
        var md5 = new Md5();
        value.CopyTo(md5.WritableSpan);
        
        return md5;
    }
    
    /// <summary>
    /// Convert the hash to a byte array.
    /// </summary>
    public byte[] ToArray()
    {
        var array = new byte[16];
        WritableSpan.CopyTo(array);
        return array;
    }

    /// <summary>
    /// Allocation free parsing of a SHA-1 hash from a hex string.
    /// </summary>
    public static Md5 ParseFromHex(string hex)
    {
        if (hex.Length != 32)
            throw new ArgumentException("The hex string must be 40 characters long.", nameof(hex));
        
        var sha = new Md5();
        Convert.FromHexString(hex, sha.WritableSpan, out _, out _);
        return sha;
    }

    /// <summary>
    /// Try to convert the hash to a hex string span.
    /// </summary>
    public bool TryToHex(Span<char> hex)
    {
        Debug.Assert(hex.Length >= 16);
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
                return new(pValue, 16);
            }
        }
    }

    /// <summary>
    /// Convert the hash to a UInt128.
    /// </summary>
    public UInt128 ToUInt128()
    {
        return MemoryMarshal.Read<UInt128>(WritableSpan);
    }

    /// <summary>
    /// Create a new MD5 hash from a UInt128.
    /// </summary>
    public static Md5 FromUInt128(UInt128 value)
    {
        var md5 = new Md5();
        MemoryMarshal.Write(md5.WritableSpan, value);
        return md5;
    }
}

internal class Md5JsonConverter : JsonConverter<Md5>
{
    public override Md5 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Md5.ParseFromHex(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, Md5 value, JsonSerializerOptions options)
    {
        Span<char> hex = stackalloc char[32];
        value.TryToHex(hex);
        writer.WriteStringValue(hex);
    }
}
