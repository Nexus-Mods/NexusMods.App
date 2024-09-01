using System.Runtime.InteropServices;
using DynamicData.Kernel;
using JetBrains.Annotations;

namespace NexusMods.CrossPlatform.Process;

/// <summary>
/// Represents ways of interacting with a runtime dependency.
/// </summary>
[PublicAPI]
public interface IRuntimeDependency
{
    /// <summary>
    /// Gets the display name of the dependency.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the description of the dependency.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets a Uri to the homepage of the dependency.
    /// </summary>
    Uri Homepage { get; }

    /// <summary>
    /// Gets all platforms on which the dependency is used.
    /// </summary>
    OSPlatform[] SupportedPlatforms { get; }

    /// <summary>
    /// Gets the type.
    /// </summary>
    RuntimeDependencyType DependencyType { get; }

    /// <summary>
    /// Queries the installation information of the dependency.
    /// </summary>
    /// <returns>None if the dependency isn't installed.</returns>
    ValueTask<Optional<RuntimeDependencyInformation>> QueryInstallationInformation(CancellationToken cancellation);
}

/// <summary>
/// Dependency Type.
/// </summary>
[PublicAPI]
public enum RuntimeDependencyType
{
    /// <summary>
    /// The dependency is an executable.
    /// </summary>
    Executable = 0,

    /// <summary>
    /// The dependency is a shared or static library.
    /// </summary>
    Library = 1,
}

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
