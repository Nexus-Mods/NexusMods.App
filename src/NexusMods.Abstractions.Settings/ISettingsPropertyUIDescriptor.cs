using JetBrains.Annotations;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Represents a descriptor for settings properties exposed to the UI.
/// </summary>
[PublicAPI]
public interface ISettingsPropertyUIDescriptor
{
    /// <summary>
    /// Gets the Section ID.
    /// </summary>
    public SectionId SectionId { get; }

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the description.
    /// </summary>
    public Func<object, string> DescriptionFactory { get; }

    /// <summary>
    /// Gets the optional link.
    /// </summary>
    public Uri? Link { get; }

    /// <summary>
    /// Gets whether changing the property requires a restart.
    /// </summary>
    public bool RequiresRestart { get; }

    /// <summary>
    /// Gets the optional custom restart message.
    /// </summary>
    /// <remarks>
    /// If this is <c>null</c>, use a default message.
    /// </remarks>
    public string? RestartMessage { get; }

    /// <summary>
    /// Gets the value container for the settings property.
    /// </summary>
    public SettingsPropertyValueContainer SettingsPropertyValueContainer { get; }
}
