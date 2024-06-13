using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Abstractions.Telemetry;

[PublicAPI]
public record TelemetrySettings : ISettings
{
    public bool IsEnabled { get; set; }

    public bool HasShownPrompt { get; set; }

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder
            .ConfigureStorageBackend<TelemetrySettings>(backend => backend.UseJson())
            .AddToUI<TelemetrySettings>(builder => builder
                .AddPropertyToUI(x => x.IsEnabled, propertyBuilder => propertyBuilder
                    .AddToSection(Sections.Privacy)
                    .WithDisplayName("Send usage data")
                    .WithDescription("Help us improve the App by sending usage data to Nexus Mods.")
                    .UseBooleanContainer()
                    .RequiresRestart()
                )
        );
    }
}
