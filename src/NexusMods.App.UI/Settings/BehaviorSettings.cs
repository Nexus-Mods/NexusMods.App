using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Settings;

public record BehaviorSettings : ISettings
{
    public bool BringWindowToFront { get; set; } = true;

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder.AddToUI<BehaviorSettings>(builder => builder
            .AddPropertyToUI(x => x.BringWindowToFront, propertyBuilder => propertyBuilder
                .AddToSection(Sections.General)
                .WithDisplayName("Bring app window to front")
                .WithDescription("When enabled, operations like adding a collection will bring the app window to the foreground")
                .UseBooleanContainer()
            )
        );
    }
}
