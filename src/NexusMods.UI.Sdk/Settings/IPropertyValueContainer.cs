using JetBrains.Annotations;
using NexusMods.Sdk.Settings;

namespace NexusMods.UI.Sdk.Settings;

/// <summary>
/// Represents a value container.
/// </summary>
[PublicAPI]
public interface IPropertyValueContainer
{
    /// <summary>
    /// Gets the current value.
    /// </summary>
    object CurrentValue { get; }

    /// <summary>
    /// Gets whether the value has changed.
    /// </summary>
    bool HasChanged { get; }

    /// <summary>
    /// Gets the validation result.
    /// </summary>
    ValidationResult ValidationResult { get; }

    /// <summary>
    /// Updates the value in the settings manager.
    /// </summary>
    void Update(ISettingsManager settingsManager);

    /// <summary>
    /// Resets the current value to the previous value.
    /// </summary>
    void ResetToPrevious();

    /// <summary>
    /// Resets the current value to the default value.
    /// </summary>
    void ResetToDefault();
}

