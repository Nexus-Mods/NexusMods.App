using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using TransparentValueObjects;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// An attribute representing an MD5 hash value.
/// </summary>
public class Md5Attribute(string ns, string name) : ScalarAttribute<Md5HashValue, UInt128, UInt128Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override UInt128 ToLowLevel(Md5HashValue value) => value.Value;

    /// <inheritdoc />
    protected override Md5HashValue FromLowLevel(UInt128 value, AttributeResolver resolver) => Md5HashValue.From(value);
}

/// <summary>
/// A value object representing an MD5 hash.
/// </summary>
[JsonConverter(typeof(Md5HashValueConverter))]
[ValueObject<UInt128>]
[DebuggerDisplay("{Hex}")]
public readonly partial struct Md5HashValue
{
    /// <summary>
    /// Parse this from a hexadecimal string.
    /// </summary>
    public static Md5HashValue Parse(string value) => From(UInt128.Parse(value, NumberStyles.HexNumber));

    /// <summary>
    /// Make a MD5 hash from a byte array.
    /// </summary>
    public static Md5HashValue From(byte[] bytes)
    {
        if (bytes.Length != 16)
            throw new ArgumentException("MD5 hash must be 16 bytes long.", nameof(bytes));

        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return From(MemoryMarshal.Read<UInt128>(bytes));
    }

    /// <summary>
    /// Hex representation.
    /// </summary>
    public string Hex => Value.ToString("x8");

    /// <inheritdoc />
    public override string ToString() => Value.ToString("x8");

}

/// <summary>
/// JSON converter for <see cref="Md5HashValue"/>.
/// </summary>
internal class Md5HashValueConverter : JsonConverter<Md5HashValue>
{
    public override Md5HashValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException();
        return Md5HashValue.Parse(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, Md5HashValue value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
