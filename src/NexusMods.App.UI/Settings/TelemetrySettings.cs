using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Settings;

public record TelemetrySettings : ISettings
{
    public bool EnableTelemetry { get; init; }

    public bool HasShownPrompt { get; init; }

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        // TODO: put in some section
        var sectionId = SectionId.DefaultValue;

        return settingsBuilder.AddToUI<TelemetrySettings>(builder => builder
            .AddPropertyToUI(x => x.EnableTelemetry, propertyBuilder => propertyBuilder
                .AddToSection(sectionId)
                .WithDisplayName("Enable Telemetry")
                .WithDescription("Send anonymous analytics information and usage data to Nexus Mods.")
                .UseBooleanContainer()
                .RequiresRestart()
            )
        );
    }
}
