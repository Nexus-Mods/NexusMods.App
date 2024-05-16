using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;
using TextMateSharp.Grammars;

namespace NexusMods.App.UI.Settings;

public record TextEditorSettings : ISettings
{
    public ThemeName ThemeName { get; [UsedImplicitly] set; } = ThemeName.Dark;

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        // TODO: put in some section
        var sectionId = SectionId.DefaultValue;

        return settingsBuilder.AddToUI<TextEditorSettings>(builder => builder
            .AddPropertyToUI(x => x.ThemeName, propertyBuilder => propertyBuilder
                .AddToSection(sectionId)
                .WithDisplayName("Text Editor Theme")
                .WithDescription("Set the desired theme in the text editor.")
                .UseSingleValueMultipleChoiceContainer(
                    valueComparer: EqualityComparer<ThemeName>.Default,
                    allowedValues: Enum.GetValues<ThemeName>(),
                    valueToDisplayString: x => x.ToString()
                )
            )
        );
    }
}
