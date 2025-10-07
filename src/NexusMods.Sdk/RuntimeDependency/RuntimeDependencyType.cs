using JetBrains.Annotations;

namespace NexusMods.Sdk;

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
