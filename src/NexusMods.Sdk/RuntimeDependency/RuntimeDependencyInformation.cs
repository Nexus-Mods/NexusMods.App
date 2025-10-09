using DynamicData.Kernel;
using JetBrains.Annotations;

namespace NexusMods.Sdk;

/// <summary>
/// Information about an installed runtime dependency.
/// </summary>
[PublicAPI]
public record RuntimeDependencyInformation
{
    /// <summary>
    /// Gets the installed version.
    /// </summary>
    public Optional<string> RawVersion { get; init; }

    /// <summary>
    /// Gets the installed version.
    /// </summary>
    public Optional<Version> Version { get; init; }
}
