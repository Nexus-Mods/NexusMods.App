using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.DataModel.JsonConverters;

public class GamePathConverter : JsonConverter<GamePath>
{
    public override GamePath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("While reading Game Path expected array start");

        reader.Read();
        var folder = Enum.Parse<GameFolderType>(reader.GetString()!);
        reader.Read();
        var path = reader.GetString()!.ToRelativePath();
        reader.Read();
        return new GamePath(folder, path);
    }

    public override void Write(Utf8JsonWriter writer, GamePath value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteStringValue(Enum.GetName(value.Type));
        writer.WriteStringValue(value.Path.ToString());
        writer.WriteEndArray();
    }
}
