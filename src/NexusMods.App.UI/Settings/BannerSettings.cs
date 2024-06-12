using System.Collections.Immutable;
using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Settings;

public record BannerSettings : ISettings
{
    public ImmutableDictionary<string, bool> BannerStatus { get; set; } = ImmutableDictionary<string, bool>.Empty;

    public bool IsDismissed(string key) => BannerStatus.GetValueOrDefault(key, false);

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder;
    }
}
