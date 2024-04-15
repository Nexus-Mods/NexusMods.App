using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Settings;

public record TelemetrySettings : ISettings
{
    public bool EnableTelemetry { get; init; }

    public bool HasShownPrompt { get; init; }

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        // TODO: show in UI
        return settingsBuilder;
    }
}
