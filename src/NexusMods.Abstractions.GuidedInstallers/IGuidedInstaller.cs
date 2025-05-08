using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;

namespace NexusMods.Abstractions.GuidedInstallers;

/// <summary>
/// Interface for a guided installer.
/// </summary>
[PublicAPI]
public interface IGuidedInstaller : IDisposable
{
    /// <summary>
    /// Sets up the installer.
    /// </summary>
    /// <param name="name">Name of the installer.</param>
    public void SetupInstaller(string name);

    /// <summary>
    /// Cleans up the installer.
    /// </summary>
    public void CleanupInstaller();

    /// <summary>
    /// Requests the user for a choice.
    /// </summary>
    public Task<UserChoice> RequestUserChoice(GuidedInstallationStep installationStep, Percent progress, CancellationToken cancellationToken);
}
