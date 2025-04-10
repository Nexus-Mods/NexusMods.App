using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Settings;

public record WelcomeSettings : ISettings
{
    public bool HasShownWelcomeMessage { get; set; }

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder) => settingsBuilder;
}
