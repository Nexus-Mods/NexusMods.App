using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.JsonConverters;

/// <inheritdoc />
public class LoadoutIdConverter : JsonConverter<LoadoutId>
{
    /// <inheritdoc />
    public override LoadoutId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return LoadoutId.FromHex(str);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, LoadoutId value, JsonSerializerOptions options)
    {
        Span<char> span = stackalloc char[32];
        value.ToHex(span);
        writer.WriteStringValue(span);
    }
}

