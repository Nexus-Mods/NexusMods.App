using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Settings;

public record LanguageSettings : ISettings
{
    [JsonConverter(typeof(CultureInfoConverter))]
    public CultureInfo UICulture { get; set; } = CultureInfo.CurrentUICulture;

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        // TODO: put in some section
        var sectionId = SectionId.DefaultValue;

        return settingsBuilder.AddToUI<LanguageSettings>(builder => builder
            .AddPropertyToUI(x => x.UICulture, propertyBuilder => propertyBuilder
                .AddToSection(sectionId)
                .WithDisplayName("Language")
                .WithDescription("Set the language for the application.")
                .UseSingleValueMultipleChoiceContainer(
                    valueComparer: EqualityComparer<CultureInfo>.Create((a,b) => a?.Equals(b) ?? false),
                    allowedValues: [
                        // TODO: dynamically get allowed values
                        new CultureInfo("en"),
                        new CultureInfo("de"),
                        new CultureInfo("pl"),
                    ],
                    valueToDisplayString: static cultureInfo => cultureInfo.DisplayName
                )
                .RequiresRestart()
            )
        );
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
