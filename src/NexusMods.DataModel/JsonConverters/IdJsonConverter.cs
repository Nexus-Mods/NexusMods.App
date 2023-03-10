using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.DataModel.Abstractions.Ids;

namespace NexusMods.DataModel.JsonConverters;

/// <inheritdoc />
public class IdJsonConverter : JsonConverter<IId>
{
    /// <inheritdoc />
    public override IId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString()!;
        var spanSize = (int)Math.Ceiling((double)str.Length / 4) * 3;
        Span<byte> span = stackalloc byte[spanSize];
        Convert.TryFromBase64String(str, span, out _);

        return IId.FromTaggedSpan(span);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IId value, JsonSerializerOptions options)
    {
        Span<byte> span = stackalloc byte[value.SpanSize + 1];
        value.ToTaggedSpan(span);
        writer.WriteBase64StringValue(span);
    }
}

