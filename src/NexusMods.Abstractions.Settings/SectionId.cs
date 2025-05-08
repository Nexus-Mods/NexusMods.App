using JetBrains.Annotations;
using TransparentValueObjects;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Represents the unique ID of a section of settings.
/// </summary>
[ValueObject<Guid>]
[PublicAPI]
public readonly partial struct SectionId : IAugmentWith<DefaultValueAugment>
{
    /// <summary>
    /// Gets the default value (empty GUID).
    /// </summary>
    public static SectionId DefaultValue { get; } = From(Guid.Empty);
}
