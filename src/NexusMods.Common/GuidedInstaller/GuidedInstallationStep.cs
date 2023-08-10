using JetBrains.Annotations;
using NexusMods.Common.GuidedInstaller.ValueObjects;

namespace NexusMods.Common.GuidedInstaller;

[PublicAPI]
public record GuidedInstallationStep
{
    public required StepId Id { get; init; }

    public required OptionGroup[] Groups { get; init; }
}
