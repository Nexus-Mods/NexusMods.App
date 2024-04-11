using System.Globalization;
using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Settings;

public record LanguageSettings : ISettings
{
    public CultureInfo UICulture { get; init; } = CultureInfo.CurrentUICulture;

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        // TODO: show in UI
        return settingsBuilder;
    }
}
