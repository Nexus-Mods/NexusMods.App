using JetBrains.Annotations;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Describes a settings section.
/// </summary>
[PublicAPI]
public record SettingsSectionSetup
{
    /// <summary>
    /// ID of the section.
    /// </summary>
    public required SectionId Id { get; init; }

    /// <summary>
    /// Section name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Section icon factory.
    /// </summary>
    public required Func<IconValue> IconFunc { get; init; }

    /// <summary>
    /// Section priority.
    /// </summary>
    /// <remarks>
    /// Sections with a higher priority will be placed higher in the UI.
    /// </remarks>
    public ushort Priority { get; init; } = 1000;

    public bool Hidden { get; init; }
}
