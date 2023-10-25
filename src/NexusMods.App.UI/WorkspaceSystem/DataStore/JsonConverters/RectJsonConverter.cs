using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using JetBrains.Annotations;

namespace NexusMods.App.UI.WorkspaceSystem;

[UsedImplicitly]
public class RectJsonConverter : JsonConverter<Rect>
{
    public override Rect Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert != typeof(Rect)) throw new JsonException();

        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();
        reader.Read();

        if (reader.TokenType != JsonTokenType.Number) throw new JsonException();
        var x = reader.GetDouble();
        reader.Read();

        if (reader.TokenType != JsonTokenType.Number) throw new JsonException();
        var y = reader.GetDouble();
        reader.Read();

        if (reader.TokenType != JsonTokenType.Number) throw new JsonException();
        var width = reader.GetDouble();
        reader.Read();

        if (reader.TokenType != JsonTokenType.Number) throw new JsonException();
        var height = reader.GetDouble();
        reader.Read();

        if (reader.TokenType != JsonTokenType.EndArray) throw new JsonException();
        return new Rect(x, y, width, height);
    }

    public override void Write(Utf8JsonWriter writer, Rect value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Width);
        writer.WriteNumberValue(value.Height);
        writer.WriteEndArray();
    }
}
