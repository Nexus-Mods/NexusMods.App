using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Sdk.Settings;

namespace NexusMods.Sdk.Tracking;

public record TrackingSettings : ISettings
{
    public static readonly Uri Link = new("https://help.nexusmods.com/article/20-privacy-policy");

    public bool EnableTracking { get; [UsedImplicitly] set; }

    public Guid DeviceId { get; set; }

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder
            .ConfigureBackend(StorageBackendOptions.Use(StorageBackends.Json))
            .ConfigureDefault(CreateDefault)
            .ConfigureProperty(
                x => x.EnableTracking,
                new PropertyOptions<TrackingSettings, bool>
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

    private static TrackingSettings CreateDefault(IServiceProvider serviceProvider)
    {
        var timeProvider = serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System;

        return new TrackingSettings
        {
            EnableTracking = false,
            DeviceId = Guid.CreateVersion7(timeProvider.GetUtcNow()),
        };
    }
}
