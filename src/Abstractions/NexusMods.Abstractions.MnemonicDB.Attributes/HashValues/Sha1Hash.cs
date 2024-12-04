using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusMods.Games.FileHashes.HashValues;

/// <summary>
/// A SHA1 hash value (20 bytes)
/// </summary>
[JsonConverter(typeof(Sha1HashJsonConverter))]
public unsafe struct Sha1Hash
{
    fixed byte _value[20];

    /// <summary>
    /// Create a new SHA1 hash value from a byte array
    /// </summary>
    public static Sha1Hash From(byte[] value)
    {
        if (value.Length != 20)
            throw new ArgumentException("Value must be 20 bytes long", nameof(value));
        
        Sha1Hash hash;
        value.AsSpan().CopyTo(new Span<byte>(hash._value, 20));
        return hash;
    }

    /// <summary>
    /// Parse a SHA1 hash value from a hex string
    /// </summary>
    public static Sha1Hash From(ReadOnlySpan<char> hexChars)
    {
        if (hexChars.Length != 40)
            throw new ArgumentException("Value must be 40 characters long", nameof(hexChars));
        
        Sha1Hash hash;
        Convert.FromHexString(hexChars, new Span<byte>(hash._value, 40), out _, out _);
        return hash;
    }
    
    /// <summary>
    /// Try to convert a SHA1 hash value to a hex string
    /// </summary>
    /// <param name="output"></param>
    public void ToHex(Span<char> output)
    {
        if (output.Length < 40)
            throw new ArgumentException("Output must be at least 40 characters long", nameof(output));
        
        fixed (byte* ptr = _value)
        {
            Convert.TryToHexString(new ReadOnlySpan<byte>(ptr, 20), output, out _);
        }
    }
}

/// <summary>
/// Sha1 hash extensions
/// </summary>
public static class Sha1HashExtensions
{
    /// <summary>
    /// Compute the SHA1 hash of a stream
    /// </summary>
    public static async Task<Sha1Hash> Sha1HashAsync(this Stream stream)
    {
        stream.Position = 0;
        using var sha1 = SHA1.Create();
        var hash = await sha1.ComputeHashAsync(stream);
        return Sha1Hash.From(hash);
    }
}

internal class Sha1HashJsonConverter : JsonConverter<Sha1Hash>
{
    public override Sha1Hash Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected a string");

        var hexChars = reader.GetString()!;
        return Sha1Hash.From(hexChars.AsSpan());
    }

    public override void Write(Utf8JsonWriter writer, Sha1Hash value, JsonSerializerOptions options)
    {
        Span<char> hexChars = stackalloc char[40];
        value.ToHex(hexChars);
        writer.WriteStringValue(hexChars);
    }
}
