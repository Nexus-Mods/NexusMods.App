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
    private const string CancelInput = "x";
    private const string BackInput = "b";
    private const string NextInput = "n";

    private static readonly string[] TableOfGroupsHeaders = { "Key", "Group" };
    private static readonly object[] TableOfGroupsFooterNextGroup = { NextInput, "Next Step" };
    private static readonly object[] TableOfGroupsFooterFinish = { NextInput, "Finish Installation" };
    private static readonly object[] TableOfGroupsFooterGoBack = { BackInput, "Previous Step" };
    private static readonly object[] TableOfGroupsFooterCancel = { CancelInput, "Cancel Installation" };

    private static readonly string[] TableOfOptionsHeaders = { "Key", "State", "Name", "Description" };
    private static readonly object[] TableOfOptionsFooterBackToGroupSelection = { BackInput, "", "Back", "Back to the group selection" };
    private static readonly object[] TableOfOptionsFooterCancel = { CancelInput, "", "Cancel", "Cancel the installation" };

    /// <summary>
    /// The renderer to use for rendering the options.
    /// </summary>
    public IRenderer Renderer { get; set; } = null!;

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
        OptionGroup? currentGroup = null;

        var selectedOptions = installationStep.Groups
            .SelectMany(group => group.Options
                .Where(option => option.Type == OptionType.PreSelected)
                .Select(option => new SelectedOption(group.Id, option.Id))
            ).ToList();

        while (true)
        {
            if (currentGroup is null)
            {
                // if the installation step has multiple groups
                // the user has to select which group they want to use

                RenderTableOfGroups(installationStep);

                var input = SkipAll ? NextInput : GetUserInput();

                switch (input)
                {
                    case CancelInput:
                        return Task.FromResult(new UserChoice(new UserChoice.CancelInstallation()));
                    case BackInput:
                        return Task.FromResult(new UserChoice(new UserChoice.GoToPreviousStep()));
                    case NextInput:
                    {
                        var requiredOptions = installationStep.Groups
                            .SelectMany(group => group.Options
                                .Where(option => option.Type == OptionType.Required)
                                .Select(option => new SelectedOption(group.Id, option.Id))
                            );

                        selectedOptions.AddRange(requiredOptions);

                        // proceed to the next step
                        return Task.FromResult(new UserChoice(new UserChoice.GoToNextStep(selectedOptions.ToArray())));
                    }
                }

                var groupIndex = ParseNumericalUserInput(input, installationStep.Groups.Length);
                if (groupIndex < 0) continue;

                currentGroup = installationStep.Groups[groupIndex];
            }
            else
            {
                // if the user has selected a group, the input will be for the options they want to use
                RenderTableOfOptions(
                    currentGroup,
                    selectedOptions
                );

                var input = GetUserInput();

                switch (input)
                {
                    case CancelInput:
                        return Task.FromResult(new UserChoice(new UserChoice.CancelInstallation()));
                    case BackInput:
                        currentGroup = null;
                        continue;
                    default:
                        UpdatedSelectedGroup(currentGroup, selectedOptions, input);
                        break;
                }
            }
        }
    }

    private void RenderTableOfGroups(GuidedInstallationStep installationStep)
    {
        var key = 1;
        var row = installationStep.Groups
            .Select(group => new object[] { key++, group.Description })
            .Append(installationStep.HasNextStep
                ? TableOfGroupsFooterNextGroup
                : TableOfGroupsFooterFinish
            );

        if (installationStep.HasPreviousStep) row = row.Append(TableOfGroupsFooterGoBack);
        row = row.Append(TableOfGroupsFooterCancel);

        var table = new Table(TableOfGroupsHeaders, row.ToArray(), "Select a Group");
        Renderer.Render(table);
    }

    private void RenderTableOfOptions(
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
                    RenderOptionState(hasSelected, option.Type),
                    option.Name,
                    option.Description
                };
            })
            .Append(TableOfOptionsFooterBackToGroupSelection)
            .Append(TableOfOptionsFooterCancel);

        var table = new Table(TableOfOptionsHeaders, row.ToArray(), group.Description);
        Renderer.Render(table);
    }

    private static string RenderOptionState(bool hasSelected, OptionType type)
    {
        if (hasSelected) return "Selected";

        Debug.Assert(Enum.IsDefined(typeof(OptionType), type));

        return type switch
        {
            OptionType.Disabled => "Disabled",
            // NOTE (erri120): These are the "initial" states.
            // The user can toggle a pre-selected option, which
            // won't change the type but the "hasSelected" value.
            OptionType.Available or OptionType.PreSelected => "Off",
            OptionType.Required => "Required",
            _ => throw new UnreachableException($"hasSelected: {hasSelected}, type: {type}")
        };
    }

    private static void UpdatedSelectedGroup(
        OptionGroup currentGroup,
        ICollection<SelectedOption> selectedOptions,
        string input)
    {
        var optionIndex = ParseNumericalUserInput(input, currentGroup.Options.Length);
        if (optionIndex < 0) return;

        var targetOption = currentGroup.Options[optionIndex];

        // can't toggle disabled or required options
        if (targetOption.Type is OptionType.Disabled or OptionType.Required) return;

        var hasSelected = selectedOptions.TryGetFirst(x => x.OptionId == targetOption.Id, out var selectedOption);
        if (hasSelected)
        {
            // the target option is already selected, the user wants to "toggle" it
            selectedOptions.Remove(selectedOption);
            return;
        }

        // the target option is not selected, the user wants to select it
        if (currentGroup.Type is OptionGroupType.ExactlyOne or OptionGroupType.AtMostOne)
        {
            // "deselect" everything
            selectedOptions.Clear();
        }

        selectedOptions.Add(new SelectedOption(currentGroup.Id, targetOption.Id));
    }

    private static string GetUserInput()
    {
        return (Console.ReadLine() ?? "").Trim();
    }

    private static int ParseNumericalUserInput(string input, int upperLimit)
    {
        try
        {
            var idx = int.Parse(input) - 1;
            if (idx >= 0 && idx < upperLimit)
                return idx;
        }
        catch (FormatException)
        {
            return -1;
        }

        return -1;
    }
}
