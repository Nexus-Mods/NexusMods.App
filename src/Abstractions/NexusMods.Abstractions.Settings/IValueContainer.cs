using JetBrains.Annotations;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Represents a value container.
/// </summary>
[PublicAPI]
public interface IValueContainer
{
    /// <summary>
    /// Gets whether the value has changed.
    /// </summary>
    bool HasChanged { get; }

    void Update(ISettingsManager settingsManager);
}
