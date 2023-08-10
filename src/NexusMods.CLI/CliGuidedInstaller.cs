using System.Diagnostics;
using JetBrains.Annotations;
using NexusMods.Abstractions.CLI;
using NexusMods.Abstractions.CLI.DataOutputs;
using NexusMods.Common;
using NexusMods.Common.GuidedInstaller;

namespace NexusMods.CLI;

/// <summary>
/// Implementation of an option selector for the CLI.
/// </summary>
[UsedImplicitly]
public class CliGuidedInstaller : IGuidedInstaller
{
    private const string ReturnInput = "x";
    private static readonly string[] TableOfOptionsHeaders = { "Key", "State", "Name", "Description" };
    private static readonly object[] TableOfOptionsFooter = { ReturnInput, "", "Back", "" };
    private static readonly string[] TableOfGroupsHeaders = { "Key", "Group" };
    private static readonly object[] TableOfGroupsFooter = { ReturnInput, "Continue" };

    /// <summary>
    /// The renderer to use for rendering the options.
    /// </summary>
    private IRenderer Renderer { get; set; } = null!;

    /// <summary>
    /// If true, all option selection will select the first choice.
    /// </summary>
    public bool SkipAll { get; set; }

    /// <inheritdoc/>
    public void SetupInstaller(string name) { }

    /// <inheritdoc/>
    public void CleanupInstaller() { }

    /// <inheritdoc />
    public Task<UserChoice> RequestUserChoice(GuidedInstallationStep installationStep, CancellationToken cancellationToken)
    {
        var currentGroup = installationStep.Groups.Length == 1
            ? installationStep.Groups[0]
            : null;

        var selectedOptions = new List<SelectedOption>();

        while (true)
        {
            RenderStep(installationStep, currentGroup, selectedOptions);
            var input = SkipAll ? "0" : GetUserInput();

            // TODO: the current implementation doesn't support selecting many options from many groups
            // see https://github.com/Nexus-Mods/NexusMods.App/issues/544

            // if the installation step has multiple groups
            // the user has to select which group they want to use
            if (currentGroup is null)
            {
                var groupIndex = ParseNumericalUserInput(input, installationStep.Groups.Length) ?? -1;
                if (groupIndex >= 0)
                {
                    currentGroup = installationStep.Groups[groupIndex];
                    selectedOptions = currentGroup.Options
                        .Where(x => x.OptionType == OptionType.PreSelected)
                        .Select(x => new SelectedOption
                        {
                            GroupId = currentGroup.Id,
                            OptionId = x.Id
                        })
                        .ToList();
                }
            }
            else
            {
                // if the user has selected a group, the input will be for the options they want to use
                if (input == ReturnInput)
                {
                    var requiredOptions = currentGroup.Options
                        .Where(x => x.OptionType == OptionType.Required)
                        .Select(x => new SelectedOption
                        {
                            GroupId = currentGroup.Id,
                            OptionId = x.Id
                        });

                    selectedOptions.AddRange(requiredOptions);

                    // proceed to the next step
                    return Task.FromResult(new UserChoice(new UserChoice.GoToNextStep(selectedOptions.ToArray())));
                }

                UpdatedSelectedGroup(currentGroup, selectedOptions, input);
            }

            if (input == ReturnInput) break;
        }

        // The user aborted the installation
        return Task.FromResult(new UserChoice(new UserChoice.CancelInstallation()));
    }

    private void RenderStep(
        GuidedInstallationStep installationStep,
        OptionGroup? currentGroup,
        IReadOnlyCollection<SelectedOption> selectedOptions)
    {
        Renderer.Render(currentGroup is null
            ? TableOfGroups(installationStep.Groups)
            : TableOfOptions(currentGroup, selectedOptions));
    }

    private static Table TableOfOptions(
        OptionGroup group,
        IReadOnlyCollection<SelectedOption> selectedOptions)
    {
        var key = 1;
        var row = group.Options
            .Select(option =>
            {
                var hasSelected = selectedOptions.Any(x => x.OptionId == option.Id);

                return new object[]
                {
                    key++,
                    RenderOptionState(hasSelected, option.OptionType),
                    option.Name,
                    option.Description
                };
            })
            .Append(TableOfOptionsFooter)
            .ToArray();

        return new Table(TableOfOptionsHeaders, row, group.Description);
    }

    private static string RenderOptionState(bool hasSelected, OptionType type)
    {
        if (hasSelected) return "Selected";

        Debug.Assert(Enum.IsDefined(typeof(OptionType), type));

        return type switch
        {
            OptionType.Disabled => "Disabled",
            OptionType.Available => "Off",
            OptionType.Required => "Required",
            _ => throw new UnreachableException()
        };
    }

    private static Table TableOfGroups(IEnumerable<OptionGroup> groups)
    {
        var key = 1;
        var row = groups
            .Select(group => new object[] { key++, group.Description })
            .Append(TableOfGroupsFooter)
            .ToArray();

        return new Table(TableOfGroupsHeaders, row, "Select a Group");
    }

    private static void UpdatedSelectedGroup(
        OptionGroup currentGroup,
        ICollection<SelectedOption> selectedOptions,
        string input)
    {
        var optionIndex = ParseNumericalUserInput(input, currentGroup.Options.Length) ?? -1;
        if (optionIndex < 0) return;

        var targetOption = currentGroup.Options[optionIndex];

        var hasSelected = selectedOptions.TryGetFirst(x => x.OptionId == targetOption.Id, out var selectedOption);
        if (hasSelected)
        {
            // the target option is already selected, the user wants to "toggle" it
            selectedOptions.Remove(selectedOption);
            return;
        }

        // the target option is not selected, the user wants to select it
        if (currentGroup.OptionGroupType is OptionGroupType.ExactlyOne or OptionGroupType.AtMostOne)
        {
            // "deselect" everything
            selectedOptions.Clear();
        }

        selectedOptions.Add(new SelectedOption
        {
            GroupId = currentGroup.Id,
            OptionId = targetOption.Id
        });
    }

    private static string GetUserInput()
    {
        return (Console.ReadLine() ?? "").Trim();
    }

    private static int? ParseNumericalUserInput(string input, int upperLimit)
    {
        try
        {
            var idx = int.Parse(input) - 1;
            if (idx >= 0 && idx < upperLimit)
                return idx;
        }
        catch (FormatException) { /* ignored */ }

        // input invalid or out of range
        return null;
    }
}
