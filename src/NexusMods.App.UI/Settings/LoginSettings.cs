using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Settings;

public record LoginSettings : ISettings
{
    public bool HasShownModal { get; set; }
    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder;
    }
}
