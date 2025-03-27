using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.DataModel.JsonConverters;

/// <inheritdoc />
public class RelativePathConverter : JsonConverter<RelativePath>
{
    /// <inheritdoc />
    public override RelativePath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString()!;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, RelativePath value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
