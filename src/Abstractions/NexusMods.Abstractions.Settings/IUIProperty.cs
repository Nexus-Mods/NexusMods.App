using JetBrains.Annotations;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Represents data about a property exposed to the UI.
/// </summary>
[PublicAPI]
public interface IUIProperty
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
    /// Gets the markdown description.
    /// </summary>
    public string Description { get; }

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
}
