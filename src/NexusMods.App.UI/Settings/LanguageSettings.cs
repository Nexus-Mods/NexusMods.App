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
                    valueComparer: CultureInfoComparer.Instance,
                    allowedValues: [
                        // TODO: dynamically get allowed values
                        new CultureInfo("en"),
                        new CultureInfo("pl"),
                        new CultureInfo("de"),
                        new CultureInfo("it"),
                    ],
                    valueToDisplayString: static cultureInfo => cultureInfo.DisplayName
                )
                .RequiresRestart()
            )
        );
    }

    private class CultureInfoComparer : IEqualityComparer<CultureInfo>
    {
        public static readonly IEqualityComparer<CultureInfo> Instance = new CultureInfoComparer();

        public bool Equals(CultureInfo? x, CultureInfo? y)
        {
            if (x is null && y is null) return true;
            if (x is null || y is null) return false;

            if (x.Equals(y)) return true;

            // en_US should equal en
            if (x.Parent.Equals(y)) return true;
            if (y.Parent.Equals(x)) return true;

            return false;
        }

        public int GetHashCode(CultureInfo obj) => throw new NotSupportedException();
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
