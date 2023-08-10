using JetBrains.Annotations;
using NexusMods.Common.GuidedInstaller.ValueObjects;

namespace NexusMods.Common.GuidedInstaller;

/// <summary>
/// Represents group of <see cref="Option"/> inside a <see cref="GuidedInstallationStep"/>.
/// </summary>
[PublicAPI]
public record OptionGroup
{
    /// <summary>
    /// Gets the unique identifier of the group.
    /// </summary>
    public required GroupId Id { get; init; }

    /// <summary>
    /// Gets the description of the group.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the type of the group.
    /// </summary>
    public required OptionGroupType Type { get; init; }

    /// <summary>
    /// Gets one or more options inside the group.
    /// </summary>
    public required Option[] Options { get; init; }
}
