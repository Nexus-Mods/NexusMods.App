using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;
using NexusMods.Sdk;

namespace NexusMods.StandardGameLocators;

public record GameLocatorSettings : ISettings
{
    public bool EnableXboxGamePass { get; [UsedImplicitly] set; } = ApplicationConstants.IsDebug;

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        if (!ApplicationConstants.IsDebug) return settingsBuilder;

        return settingsBuilder
            .ConfigureStorageBackend<GameLocatorSettings>(builder => builder.UseJson())
            .AddToUI<GameLocatorSettings>(builder => builder
                .AddPropertyToUI(x => x.EnableXboxGamePass, propertyBuilder => propertyBuilder
                    .AddToSection(Sections.Experimental)
                    .WithDisplayName("Enable Xbox Game Pass support")
                    .WithDescription("Allows you to manage games installed with Xbox Game Pass.")
                    .UseBooleanContainer()
                    .RequiresRestart()
                )
            );
    }
}
