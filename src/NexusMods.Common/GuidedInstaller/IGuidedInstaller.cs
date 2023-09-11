using JetBrains.Annotations;

namespace NexusMods.Common.GuidedInstaller;

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
    /// <param name="installationStep"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<UserChoice> RequestUserChoice(GuidedInstallationStep installationStep, CancellationToken cancellationToken);
}
