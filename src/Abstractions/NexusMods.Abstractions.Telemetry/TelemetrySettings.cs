using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Abstractions.Telemetry;

[PublicAPI]
public record TelemetrySettings : ISettings
{
    public bool IsEnabled { get; set; }

    public bool HasShownPrompt { get; set; }

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        // TODO: put in some section
        var sectionId = SectionId.DefaultValue;

        return settingsBuilder.AddToUI<TelemetrySettings>(builder => builder
            .AddPropertyToUI(x => x.IsEnabled, propertyBuilder => propertyBuilder
                .AddToSection(sectionId)
                .WithDisplayName("Enable Telemetry")
                .WithDescription("Send anonymous analytics information and usage data to Nexus Mods.")
                .UseBooleanContainer()
                .RequiresRestart()
            )
        );
    }
}
