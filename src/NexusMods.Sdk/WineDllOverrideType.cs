using JetBrains.Annotations;

namespace NexusMods.Sdk;

/// <summary>
/// DLL override types for Wine.
/// </summary>
[PublicAPI]
public enum WineDllOverrideType
{
    /// <summary>
    /// The dll is disabled.
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// The built-in version should be loaded.
    /// </summary>
    BuiltIn = 1,

    /// <summary>
    /// The native version should be loaded.
    /// </summary>
    Native = 2,
}
