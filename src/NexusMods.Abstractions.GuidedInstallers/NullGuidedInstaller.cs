
using NexusMods.Sdk.Jobs;

namespace NexusMods.Abstractions.GuidedInstallers;

/// <summary>
/// A empty implementation of <see cref="IGuidedInstaller"/>. All it does is throw an error if the RequestUserChoice method is called.
/// </summary>
public class NullGuidedInstaller : IGuidedInstaller
{
    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public void SetupInstaller(string name)
    {
        throw new NotSupportedException("Cannot setup the installer, this is a null implementation");
    }

    /// <inheritdoc />
    public void CleanupInstaller()
    {
        throw new NotSupportedException("Cannot cleanup the installer, this is a null implementation");
    }

    /// <inheritdoc />
    public Task<UserChoice> RequestUserChoice(GuidedInstallationStep installationStep, Percent progress, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("This installer does not support user interaction, it is only meant to be used for automated installations and testing");
    }
}
