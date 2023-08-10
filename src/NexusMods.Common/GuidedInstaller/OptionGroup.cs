using JetBrains.Annotations;

namespace NexusMods.Common.GuidedInstaller;

[PublicAPI]
public record OptionGroup
{
    public required ValueObjects.GroupId Id { get; init; }

    public required string Description { get; init; }

    public required OptionGroupType OptionGroupType { get; init; }

    public required Option[] Options { get; init; }
}
