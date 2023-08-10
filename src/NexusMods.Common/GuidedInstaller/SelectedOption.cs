using JetBrains.Annotations;
using NexusMods.Common.GuidedInstaller.ValueObjects;

namespace NexusMods.Common.GuidedInstaller;

[PublicAPI]
public readonly struct SelectedOption
{
    public readonly GroupId GroupId { get; init; }

    public readonly OptionId OptionId { get; init; }

    public SelectedOption(GroupId groupId, OptionId optionId)
    {
        GroupId = groupId;
        OptionId = optionId;
    }
}
