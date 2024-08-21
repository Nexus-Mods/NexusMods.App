using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Settings;

public record ExperimentalViewSettings : ISettings
{
    public bool ShowNewLibraryView { get; [UsedImplicitly] set; }
    public bool ShowNewLoadoutView { get; [UsedImplicitly] set; }

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder.AddToUI<ExperimentalViewSettings>(settingsUIBuilder => settingsUIBuilder
            .AddPropertyToUI(settings => settings.ShowNewLibraryView, propertyUIBuilder => propertyUIBuilder
                .AddToSection(Sections.Experimental)
                .WithDisplayName("Show new library view")
                .WithDescription("Enables the new library view")
                .UseBooleanContainer()
            )
            .AddPropertyToUI(settings => settings.ShowNewLoadoutView, propertyUIBuilder => propertyUIBuilder
                .AddToSection(Sections.Experimental)
                .WithDisplayName("Show new loadout view")
                .WithDescription("Enables the new loadout view")
                .UseBooleanContainer()
            )
        );
    }
}
