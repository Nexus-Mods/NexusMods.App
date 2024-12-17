using System.Buffers;
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

        var status = Convert.FromHexString(input, bytes, out _, out _);
        Debug.Assert(status == OperationStatus.Done);

        var value = MemoryMarshal.Read<ulong>(bytes);
        return Hash.FromULong(value);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Hash value, JsonSerializerOptions options)
    {
        Span<char> span = stackalloc char[sizeof(ulong) * 2];
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        MemoryMarshal.Write(bytes, value.Value);

        var success = Convert.TryToHexString(bytes, span, out _);
        Debug.Assert(success);

        writer.WriteStringValue(span);
    }
}
