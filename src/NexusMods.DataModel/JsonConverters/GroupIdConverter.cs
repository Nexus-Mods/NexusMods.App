using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace NexusMods.DataModel.JsonConverters;

/// <inheritdoc />
[PublicAPI]
public class GroupIdConverter : JsonConverter<GroupId>
{
    /// <inheritdoc />
    public override GroupId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var data = reader.GetBytesFromBase64();
        return GroupId.From(new Guid(data));
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, GroupId value, JsonSerializerOptions options)
    {
        Span<byte> span = stackalloc byte[16];
        value.Value.TryWriteBytes(span);
        writer.WriteBase64StringValue(span);
    }
}
