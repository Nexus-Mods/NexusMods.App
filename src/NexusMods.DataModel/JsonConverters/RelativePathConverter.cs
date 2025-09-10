using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.DataModel.JsonConverters;

/// <inheritdoc />
public class RelativePathConverter : JsonConverter<RelativePath>
{
    /// <inheritdoc />
    public override RelativePath ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var path = reader.GetString();
        if (path is null) throw new JsonException();

        return path;
    }

    /// <inheritdoc />
    public override void WriteAsPropertyName(Utf8JsonWriter writer, RelativePath value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    /// <inheritdoc />
    public override RelativePath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var path = reader.GetString();
        if (path is null) throw new JsonException();

        return path;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, RelativePath value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
