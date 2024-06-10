using JetBrains.Annotations;
using NexusMods.Icons;

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
}
