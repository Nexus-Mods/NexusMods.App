using JetBrains.Annotations;
using NexusMods.Sdk.Settings;

namespace NexusMods.Abstractions.Telemetry;

[PublicAPI]
public record TelemetrySettings : ISettings
{
    public bool IsEnabled { get; set; }

    public static readonly Uri Link = new("https://help.nexusmods.com/article/20-privacy-policy");

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder
            .ConfigureBackend(StorageBackendOptions.Use(StorageBackends.Json))
            .ConfigureProperty(
                x => x.IsEnabled,
                new PropertyOptions<TelemetrySettings, bool>
                {
                    Section = Sections.Privacy,
                    DisplayName = "Send diagnostic and usage data",
                    DescriptionFactory = _ => "Help us improve the app by sending diagnostic and usage data to Nexus Mods.",
                    HelpLink = Link,
                    RequiresRestart = true,
                },
                new BooleanContainerOptions()
            );
    }
}
