using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Humanizer;
using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Settings;

public record LanguageSettings : ISettings
{
    [JsonConverter(typeof(CultureInfoConverter))]
    public CultureInfo UICulture { get; set; } = CultureInfo.CurrentUICulture;

    private static readonly CultureInfo[] SupportedLanguages;

    static LanguageSettings()
    {
        // TODO: dynamically get allowed values
        CultureInfo[] supportedLanguages =
        [
            new("en"),
            new("pl"),
            new("de"),
            new("it"),
            new("pt-br"),
        ];

        Array.Sort(supportedLanguages, (a, b) => string.Compare(a.NativeName, b.NativeName, StringComparison.InvariantCulture));
        SupportedLanguages = supportedLanguages;
    }

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder.AddToUI<LanguageSettings>(builder => builder
            .AddPropertyToUI(x => x.UICulture, propertyBuilder => propertyBuilder
                .AddToSection(Sections.General)
                .WithDisplayName("Language")
                .WithDescription("Set the language for the application.")
                .UseSingleValueMultipleChoiceContainer(
                    valueComparer: CultureInfoComparer.Instance,
                    allowedValues: SupportedLanguages,
                    valueToDisplayString: static cultureInfo => To.TitleCase.Transform(cultureInfo.NativeName, culture: CultureInfo.InvariantCulture)
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
