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
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("While reading Game Path expected object start");

        reader.Read();
        var folder = GameFolderType.From(reader.GetInt32());
        reader.Read();
        var path = reader.GetString()!.ToRelativePath();
        reader.Read();
        return new GamePath(folder, path);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, GamePath value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumberValue(value.Type.Value);
        writer.WriteStringValue(value.Path.ToString());
        writer.WriteEndObject();
    }
}
