using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Abstractions.Telemetry;

[PublicAPI]
public record TelemetrySettings : ISettings
{
    public bool IsEnabled { get; set; }

    public bool HasShownPrompt { get; set; }

    public static readonly Uri Link = new("https://help.nexusmods.com/article/20-privacy-policy");

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder
            .ConfigureStorageBackend<TelemetrySettings>(backend => backend.UseJson())
            .AddToUI<TelemetrySettings>(builder => builder
                .AddPropertyToUI(x => x.IsEnabled, propertyBuilder => propertyBuilder
                    .AddToSection(Sections.Privacy)
                    .WithDisplayName("Send diagnostic and usage data")
                    .WithDescription("Help us improve the app by sending diagnostic and usage data to Nexus Mods.")
                    .WithLink(Link)
                    .UseBooleanContainer()
                    .RequiresRestart()
                )
        );
    }
}
