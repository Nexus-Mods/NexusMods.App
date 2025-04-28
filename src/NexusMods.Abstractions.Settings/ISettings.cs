using JetBrains.Annotations;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Interface for settings.
/// </summary>
[PublicAPI]
public interface ISettings
{
    /// <summary>
    /// Configuration method for this type.
    /// </summary>
    static abstract ISettingsBuilder Configure(ISettingsBuilder settingsBuilder);
}
