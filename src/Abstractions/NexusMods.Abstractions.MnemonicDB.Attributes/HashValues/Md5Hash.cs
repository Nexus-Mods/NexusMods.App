using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using TransparentValueObjects;

namespace NexusMods.Games.FileHashes.HashValues;

/// <summary>
/// A value object representing an MD5 hash.
/// </summary>
[JsonConverter(typeof(Md5HashValueConverter))]
[ValueObject<UInt128>]
public readonly partial struct Md5Hash
{
    /// <summary>
    /// Parse this from a hexadecimal string.
    /// </summary>
    public static Md5Hash Parse(string value) => 
        From(UInt128.Parse(value, NumberStyles.HexNumber));

    /// <summary>
    /// Make a MD5 hash from a byte array.
    /// </summary>
    public static Md5Hash From(byte[] bytes)
    {
        if (bytes.Length != 16)
            throw new ArgumentException("MD5 hash must be 16 bytes long.", nameof(bytes));

        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return From(MemoryMarshal.Read<UInt128>(bytes));
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString("x8");

}

/// <summary>
/// Md5 extensions methods.
/// </summary>
public static class Md5Extensions
{
    /// <summary>
    /// Compute the MD5 hash of a stream.
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static async Task<Md5Hash> Md5HashAsync(this Stream stream)
    {
        var md5 = MD5.Create();
        stream.Position = 0;
        var hash = await md5.ComputeHashAsync(stream);
        return Md5Hash.From(hash);
    }
}

/// <summary>
/// JSON converter for <see cref="Md5Hash"/>.
/// </summary>
internal class Md5HashValueConverter : JsonConverter<Md5Hash>
{
    public override Md5Hash Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException();
        return Md5Hash.Parse(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, Md5Hash value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
