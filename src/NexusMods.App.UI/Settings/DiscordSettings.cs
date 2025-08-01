using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Settings;

public record DiscordSettings : ISettings
{
    public bool EnableRichPresence { get; set; } = true;

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder.AddToUI<DiscordSettings>(builder => builder
            .AddPropertyToUI(x => x.EnableRichPresence, propertyBuilder => propertyBuilder
                .AddToSection(Sections.General)
                .WithDisplayName("Use Discord Rich Presence")
                .WithDescription("Show which game you are modding using Discord Rich Presence.")
                .UseBooleanContainer()
                .RequiresRestart()
            )
        );
    }
}
