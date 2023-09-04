using System.Collections;
using NexusMods.Common.GuidedInstaller.ValueObjects;

namespace NexusMods.Common.GuidedInstaller;

/// <summary>
/// Validation utilities for <see cref="GuidedInstallationStep"/>.
/// </summary>
public static class GuidedInstallerValidation
{
    /// <summary>
    /// Checks if the <see cref="OptionGroup"/>s in the <see cref="GuidedInstallationStep"/> have a valid selection.
    /// </summary>
    /// <remarks>
    /// A group is valid if its selection satisfies the <see cref="OptionGroup.Type"/> requirements.
    /// </remarks>
    /// <param name="installationStep">The installation step containing all the groups</param>
    /// <param name="selectedOptions">A collection of <see cref="SelectedOption"/> for all the groups.</param>
    /// <returns>
    /// A collection of <see cref="GroupId"/>s that have invalid selections.
    /// An empty collection means all groups have valid selections.
    /// </returns>
    public static IEnumerable<GroupId> ValidateStepSelections(GuidedInstallationStep installationStep,
        IEnumerable<SelectedOption> selectedOptions)
    {
        var selectedOptionsList = selectedOptions.ToArray();

        return (from @group in installationStep.Groups
            where !IsValidGroupSelection(@group, selectedOptionsList)
            select @group.Id).ToList();
    }

    /// <summary>
    /// Checks if the <see cref="OptionGroup"/> has a valid selection.
    /// </summary>
    /// <remarks>
    /// A selection is valid if it satisfies the group's <see cref="OptionGroup.Type"/> requirements.
    /// </remarks>
    /// <param name="group"></param>
    /// <param name="selectedOptions">
    /// A collection of <see cref="SelectedOption"/>, could contain selections for other groups as well
    /// </param>
    /// <returns>True if the group has a valid selection, false otherwise</returns>
    public static bool IsValidGroupSelection(OptionGroup group, IEnumerable<SelectedOption> selectedOptions)
    {
        var selectedOptionsList = selectedOptions.ToArray();
        return group.Type switch
        {
            OptionGroupType.ExactlyOne => selectedOptionsList.Count(
                selectedOption => selectedOption.GroupId == group.Id) == 1,

            OptionGroupType.AtMostOne =>
                selectedOptionsList.Count(selectedOption => selectedOption.GroupId == group.Id) <= 1,

            OptionGroupType.AtLeastOne =>
                selectedOptionsList.Any(selectedOption => selectedOption.GroupId == group.Id),

            _ => true
        };
    }
}
