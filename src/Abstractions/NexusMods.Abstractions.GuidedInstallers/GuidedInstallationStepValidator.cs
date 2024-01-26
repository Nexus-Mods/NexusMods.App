using NexusMods.Abstractions.GuidedInstallers.ValueObjects;

namespace NexusMods.Abstractions.GuidedInstallers;

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
    /// Note: Required options should be included in the <paramref name="selectedOptions"/> collection.
    /// </remarks>
    /// <param name="installationStep">The installation step containing all the groups</param>
    /// <param name="selectedOptions">A collection of <see cref="SelectedOption"/> for all the groups.</param>
    /// <returns>
    /// An array of <see cref="GroupId"/>s that have invalid selections.
    /// An empty array means all groups have valid selections.
    /// </returns>
    public static GroupId[] ValidateStepSelections(
        GuidedInstallationStep installationStep,
        IReadOnlyCollection<SelectedOption> selectedOptions)
    {
        return installationStep.Groups
            .Where(group => !IsValidGroupSelection(group, selectedOptions))
            .Select(group => group.Id)
            .ToArray();
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
        return group.Type switch
        {
            OptionGroupType.ExactlyOne => selectedOptions.Count(selectedOption => selectedOption.GroupId == group.Id) == 1,
            OptionGroupType.AtMostOne => selectedOptions.Count(selectedOption => selectedOption.GroupId == group.Id) <= 1,
            OptionGroupType.AtLeastOne => selectedOptions.Any(selectedOption => selectedOption.GroupId == group.Id),
            _ => true
        };
    }
}
