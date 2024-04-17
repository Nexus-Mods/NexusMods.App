using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Settings;

public record LanguageSettings : ISettings
{
    [JsonConverter(typeof(CultureInfoConverter))]
    public CultureInfo UICulture { get; init; } = CultureInfo.CurrentUICulture;

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        // TODO: show in UI
        return settingsBuilder;
    }
}

public class CultureInfoConverter : JsonConverter<CultureInfo>
{
    public override CultureInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var name = reader.GetString();
        if (name is null) return null;

        var res = CultureInfo.GetCultureInfo(name);
        return res;
    }

    public override void Write(Utf8JsonWriter writer, CultureInfo value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Name);
    }
}
