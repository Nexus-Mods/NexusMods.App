using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusMods.Sdk.Hashes;

/// <summary>
/// Custom JSON converter for <see cref="Crc32Value"/>. We're not using augments here as we want hex strings not
/// the raw base 10 value.
/// </summary>
internal class Crc32JsonConverter : JsonConverter<Crc32Value>
{
    public override Crc32Value Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var input = reader.GetString();
        if (input is null) throw new JsonException();

        Span<byte> bytes = stackalloc byte[sizeof(uint)];

        var status = Convert.FromHexString(input, bytes, out _, out _);
        Debug.Assert(status == OperationStatus.Done);

        var value = MemoryMarshal.Read<uint>(bytes);
        return Crc32Value.From(value);
    }

    public override void Write(Utf8JsonWriter writer, Crc32Value value, JsonSerializerOptions options)
    {
        Span<char> span = stackalloc char[sizeof(uint) * 2];
        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        MemoryMarshal.Write(bytes, value.Value);

        var success = Convert.TryToHexString(bytes, span, out _);
        Debug.Assert(success);

        writer.WriteStringValue(span);
    }
}
