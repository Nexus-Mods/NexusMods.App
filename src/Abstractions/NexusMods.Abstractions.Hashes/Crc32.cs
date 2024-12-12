using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Hashing.xxHash3;
using TransparentValueObjects;

namespace NexusMods.Abstractions.Hashes;

/// <summary>
/// A value representing a 32-bit Cyclic Redundancy Check (CRC) hash.
/// </summary>
[JsonConverter(typeof(Crc32JsonConverter))]
[ValueObject<uint>]
public readonly partial struct Crc32
{
}

/// <summary>
/// Custom JSON converter for <see cref="Crc32"/>. We're not using augments here as we want hex strings not
/// the raw base 10 value.
/// </summary>
internal class Crc32JsonConverter : JsonConverter<Crc32>
{
    public override Crc32 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Span<byte> chars = stackalloc byte[4];
        Convert.FromHexString(reader.GetString()!, chars, out _, out _);
        return Crc32.From(MemoryMarshal.Read<uint>(chars));
    }

    public override void Write(Utf8JsonWriter writer, Crc32 value, JsonSerializerOptions options)
    {
        Span<char> span = stackalloc char[8];
        Span<byte> bytes = stackalloc byte[4];
        MemoryMarshal.Write(bytes, value.Value);
        Convert.TryToHexString(bytes, span, out _);
        writer.WriteStringValue(span);
    }
}
