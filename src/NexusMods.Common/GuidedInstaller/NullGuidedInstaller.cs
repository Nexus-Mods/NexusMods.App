using NexusMods.Abstractions.Values;

namespace NexusMods.Common.GuidedInstaller;

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
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void CleanupInstaller()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<UserChoice> RequestUserChoice(GuidedInstallationStep installationStep, Percent progress, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("This installer does not support user interaction, it is only meant to be used for automated installations and testing");
    }
}
