using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Abstractions.GameLocators;
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

        var locationIdString = reader.GetString();
        var folder = locationIdString is null ? LocationId.Unknown : LocationId.From(locationIdString);

        reader.Read();
        var path = reader.GetString()!;
        reader.Read();
        return new GamePath(folder, path);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, GamePath value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteStringValue(value.LocationId.ToString());
        writer.WriteStringValue(value.Path.ToString());
        writer.WriteEndArray();
    }
}
