using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;
using NexusMods.App.BuildInfo;

namespace NexusMods.StandardGameLocators;

public record GameLocatorSettings : ISettings
{
    public bool EnableXboxGamePass { get; [UsedImplicitly] set; } = CompileConstants.IsDebug;

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        // TODO: put in some section
        var sectionId = SectionId.DefaultValue;

        return settingsBuilder
            .ConfigureStorageBackend<GameLocatorSettings>(builder => builder.UseJson())
            .AddToUI<GameLocatorSettings>(builder => builder
                .AddPropertyToUI(x => x.EnableXboxGamePass, propertyBuilder => propertyBuilder
                    .AddToSection(sectionId)
                    .WithDisplayName("(Experimental) Enable Xbox Game Pass support")
                    .WithDescription("For testing the Xbox Game Pass detection")
                    .UseBooleanContainer()
                )
            );
    }
}
