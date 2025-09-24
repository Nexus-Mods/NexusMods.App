using JetBrains.Annotations;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Interface for settings.
/// </summary>
[PublicAPI]
[Obsolete("Replaced with new settings API")]
public interface ISettings
{
    /// <summary>
    /// Configuration method for this type.
    /// </summary>
    static abstract ISettingsBuilder Configure(ISettingsBuilder settingsBuilder);
}
