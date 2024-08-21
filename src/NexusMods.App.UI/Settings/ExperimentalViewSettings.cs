using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Settings;

public record ExperimentalViewSettings : ISettings
{
    public bool ShowNewTreeViews { get; [UsedImplicitly] set; }

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder.AddToUI<ExperimentalViewSettings>(settingsUIBuilder => settingsUIBuilder
            .AddPropertyToUI(settings => settings.ShowNewTreeViews, propertyUIBuilder => propertyUIBuilder
                .AddToSection(Sections.Experimental)
                .WithDisplayName("Enable tree UI for Library and My Mods")
                .WithDescription("""Adds duplicate pages for "My Mods" and "Library" that uses the in-progress tree UI.""")
                .UseBooleanContainer()
            )
        );
    }
}
