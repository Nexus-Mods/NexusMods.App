using JetBrains.Annotations;
using NexusMods.Common.GuidedInstaller.ValueObjects;

namespace NexusMods.Common.GuidedInstaller;

/// <summary>
/// Represents a single step during installation.
/// </summary>
[PublicAPI]
public record GuidedInstallationStep
{
    /// <summary>
    /// Gets the unique identifier of the step.
    /// </summary>
    public required StepId Id { get; init; }

    /// <summary>
    /// Gets the name of the step.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets one or multiple option groups.
    /// </summary>
    public required OptionGroup[] Groups { get; init; }

    /// <summary>
    /// Gets whether or not there is a previous step.
    /// </summary>
    public bool HasPreviousStep { get; init; }

    /// <summary>
    /// Gets whether or not there is a next step.
    /// </summary>
    public bool HasNextStep { get; init; }
}
