using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Settings;

public class UpdaterSettings : ISettings
{
    public Version VersionToSkip { get; [UsedImplicitly] set; } = new(0, 0, 0);

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder;
    }
}
