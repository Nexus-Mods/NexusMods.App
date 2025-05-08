using JetBrains.Annotations;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace NexusMods.Abstractions.Games;

/// <summary>
/// List of features.
/// </summary>
[PublicAPI]
public static class BaseFeatures
{
    public static readonly Feature GameLocatable = new(Description: "The game can be located.");

    public static readonly Feature HasInstallers = new(Description: "The extension provides mod installers.");

    public static readonly Feature HasDiagnostics = new(Description: "The extension provides diagnostics.");

    public static readonly Feature HasLoadOrder = new(Description: "The extension provides load-order support.");
}
