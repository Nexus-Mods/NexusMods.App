using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
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
    /// <inheritdoc/>
    public override Hash Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var input = reader.GetString();
        if (input is null) throw new JsonException();
        
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        ((ReadOnlySpan<char>)input).FromHex(bytes);
        
        var value = MemoryMarshal.Read<ulong>(bytes);
        return Hash.FromULong(value);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Hash value, JsonSerializerOptions options)
    {
        Span<byte> buffer = stackalloc byte[8];
        MemoryMarshal.Write(buffer, value.Value);
        
        Span<char> span = stackalloc char[sizeof(ulong) * 2];
        ((ReadOnlySpan<byte>)buffer).ToHex(span);
        
        writer.WriteStringValue(span);
    }
}
