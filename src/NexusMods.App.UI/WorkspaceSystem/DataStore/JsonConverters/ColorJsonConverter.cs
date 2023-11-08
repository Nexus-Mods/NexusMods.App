using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Media;
using JetBrains.Annotations;

namespace NexusMods.App.UI.WorkspaceSystem;

[UsedImplicitly]
public class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String) throw new JsonException();
        var s = reader.GetString();
        return s is null ? Colors.Black : Color.Parse(s);
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
