using JetBrains.Annotations;

namespace NexusMods.Abstractions.Settings;

[PublicAPI]
public interface ISettings
{
    static abstract ISettingsBuilder Configure(ISettingsBuilder settingsBuilder);
}
