using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Settings;

public record LoadoutGridSettings : ISettings
{
    public bool ShowGameFiles { get; [UsedImplicitly] set; }

    public bool ShowOverride { get; [UsedImplicitly] set; }

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder.AddToUI<LoadoutGridSettings>(builder => builder
            .AddPropertyToUI(x => x.ShowGameFiles, propertyBuilder => propertyBuilder
                .AddToSection(Sections.Advanced)
                .WithDisplayName("Show game files")
                .WithDescription("Show game files as a mod alongside your added mods.")
                .UseBooleanContainer()
            )
            .AddPropertyToUI(x => x.ShowOverride, propertyBuilder => propertyBuilder
                .AddToSection(Sections.Advanced)
                .WithDisplayName("Show Override mod")
                .WithDescription("Shows the Override mod, which contains files generated or modified during gameplay that aren't part of any specific mod.")
                .UseBooleanContainer()
            )
        );
    }
}
