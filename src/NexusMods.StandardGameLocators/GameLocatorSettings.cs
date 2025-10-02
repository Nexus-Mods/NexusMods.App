using JetBrains.Annotations;
using NexusMods.Sdk.Settings;
using NexusMods.Sdk;

namespace NexusMods.StandardGameLocators;

public record GameLocatorSettings : ISettings
{
    public bool EnableXboxGamePass { get; [UsedImplicitly] set; } = ApplicationConstants.IsDebug;

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder
            .ConfigureBackend(StorageBackendOptions.Use(StorageBackends.Json))
            .ConfigureProperty(
                x => x.EnableXboxGamePass,
                new PropertyOptions<GameLocatorSettings, bool>
                {
                    Section = Sections.Experimental,
                    DisplayName = "Enable Xbox Game Pass support",
                    DescriptionFactory = _ => "Allows you to manage games installed with Xbox Game Pass.",
                    RequiresRestart = true,
                },
                new BooleanContainerOptions()
            );
    }
}
