using JetBrains.Annotations;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Container for a boolean value.
/// </summary>
[PublicAPI]
public sealed class BooleanContainer : APropertyValueContainer<bool>
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public BooleanContainer(bool value, bool defaultValue, Action<ISettingsManager, bool> updaterFunc)
        : base(value, defaultValue, updaterFunc) { }
}
