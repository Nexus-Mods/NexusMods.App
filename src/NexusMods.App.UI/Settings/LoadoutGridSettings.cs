using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Settings;

public record LoadoutGridSettings : ISettings
{
    public bool ShowGameFiles { get; set; }

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        // TODO: put in some section
        var sectionId = SectionId.DefaultValue;

        return settingsBuilder.AddToUI<LoadoutGridSettings>(builder => builder
            .AddPropertyToUI(x => x.ShowGameFiles, propertyBuilder => propertyBuilder
                .AddToSection(sectionId)
                .WithDisplayName("Show Game Files")
                .WithDescription("Shows the Game Files in the Mods page.")
                .UseBooleanContainer())
        );
    }
}
