using JetBrains.Annotations;
using NexusMods.Abstractions.GuidedInstallers.ValueObjects;

namespace NexusMods.Abstractions.GuidedInstallers;

/// <summary>
/// Represents a selected option.
/// </summary>
[PublicAPI]
public readonly struct SelectedOption
{
    /// <summary>
    /// Gets the unique identifier of the group that contains the selected option.
    /// </summary>
    public readonly GroupId GroupId;

    /// <summary>
    /// Gets the unique identifier of the selected option.
    /// </summary>
    public readonly OptionId OptionId;

    /// <summary>
    /// Constructor.
    /// </summary>
    public SelectedOption(GroupId groupId, OptionId optionId)
    {
        GroupId = groupId;
        OptionId = optionId;
    }
}
