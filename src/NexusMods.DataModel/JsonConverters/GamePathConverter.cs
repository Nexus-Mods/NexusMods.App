using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.DataModel.JsonConverters;

/// <inheritdoc />
public class GamePathConverter : JsonConverter<GamePath>
{
    /// <inheritdoc />
    public override GamePath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("While reading Game Path expected object start");

        reader.Read();
        var folder = LocationId.From(reader.GetString());
        reader.Read();
        var path = reader.GetString()!.ToRelativePath();
        reader.Read();
        return new GamePath(folder, path);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, GamePath value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteStringValue(value.Type.Value);
        writer.WriteStringValue(value.Path.ToString());
        writer.WriteEndArray();
    }
}
