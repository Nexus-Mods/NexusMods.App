using NexusMods.Abstractions.Library.Installers;

namespace NexusMods.Games.AdvancedInstaller;

/// <summary>
/// Handles the implementation of the <see cref="AdvancedManualInstaller"/> functionality.
/// <remarks>
/// Implementations for this interface are meant to be obtained from DI.
/// If implementation is not available, the functionality is not supported,
/// for example, if executing in CLI without UI.
/// </remarks>
/// </summary>
public interface IAdvancedInstallerHandler : ILibraryArchiveInstaller
{
    /// <summary>
    /// Whether the advanced installer was opened directly or as a fallback.
    /// </summary>
    bool WasOpenedDirectly { get; set; }
}
