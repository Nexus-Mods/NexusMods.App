using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Hashing.xxHash3;

namespace NexusMods.Abstractions.Hashes;

/// <summary>
/// Json Converter for <see cref="Hash"/>.
/// </summary>
public class HashJsonConverter : JsonConverter<Hash>
{
    public override Hash Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Span<byte> chars = stackalloc byte[8];
        Convert.FromHexString(reader.GetString()!, chars, out _, out _);
        return Hash.FromULong(MemoryMarshal.Read<ulong>(chars));
    }

    public override void Write(Utf8JsonWriter writer, Hash value, JsonSerializerOptions options)
    {
        Span<char> span = stackalloc char[16];
        Span<byte> bytes = stackalloc byte[8];
        MemoryMarshal.Write(bytes, value.Value);
        Convert.TryToHexString(bytes, span, out _);
        writer.WriteStringValue(span);
    }
}
