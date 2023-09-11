using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.CLI;
using NexusMods.Abstractions.CLI.DataOutputs;
using NexusMods.Common;
using NexusMods.Common.GuidedInstaller;

namespace NexusMods.CLI;

/// <summary>
/// Implementation of an option selector for the CLI.
/// </summary>
[UsedImplicitly]
public sealed class CliGuidedInstaller : IGuidedInstaller
{
    private const string CancelInput = "x";
    private const string PreviousInput = "p";
    private const string BackInput = "b";
    private const string NextInput = "n";

    private static readonly string[] TableOfGroupsHeaders = { "Key", "Group" };
    private static readonly object[] TableOfGroupsFooterNextStep = { NextInput, "Next Step" };
    private static readonly object[] TableOfGroupsFooterFinish = { NextInput, "Finish Installation" };
    private static readonly object[] TableOfGroupsFooterPreviousStep = { PreviousInput, "Previous Step" };
    private static readonly object[] TableOfGroupsFooterCancel = { CancelInput, "Cancel Installation" };

    private static readonly string[] TableOfOptionsHeaders = { "Key", "State", "Name", "Description" };

    private static readonly object[] TableOfOptionsFooterBackToGroupSelection =
        { BackInput, "", "Back", "Back to the group selection" };

    private static readonly object[] TableOfOptionsFooterCancel =
        { CancelInput, "", "Cancel", "Cancel the installation" };

    private readonly ILogger<CliGuidedInstaller> _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    public CliGuidedInstaller(ILogger<CliGuidedInstaller> logger)
    {
        _logger = logger;
    }

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
    public Task<UserChoice> RequestUserChoice(GuidedInstallationStep installationStep,
        CancellationToken cancellationToken)
    {
        OptionGroup? currentGroup = null;

        // Select all pre-selected and required options
        var selectedOptions = installationStep.Groups
            .SelectMany(group => group.Options
                .Where(option => option.Type is OptionType.PreSelected or OptionType.Required)
                .Select(option => new SelectedOption(group.Id, option.Id))
            ).ToList();

        while (true)
        {
            if (currentGroup is null)
            {
                // the user hasn't selected a group yet
                RenderTableOfGroups(installationStep);

                var input = SkipAll ? NextInput : GetUserInput();

                if (SkipAll)
                {
                    // For AtLeastOne and ExactlyOne groups, make sure at least one option is selected.
                    var atLeastOneGroups = installationStep.Groups
                        .Where(group => group.Type is OptionGroupType.AtLeastOne or OptionGroupType.ExactlyOne)
                        .ToArray();

                    foreach (var atLeastOneGroup in atLeastOneGroups)
                    {
                        if (selectedOptions.Any(x => x.GroupId == atLeastOneGroup.Id)) continue;
                        selectedOptions.Add(new SelectedOption(atLeastOneGroup.Id, atLeastOneGroup.Options[0].Id));
                    }
                }

                switch (input)
                {
                    case CancelInput:
                        return Task.FromResult(new UserChoice(new UserChoice.CancelInstallation()));
                    case PreviousInput:
                        return Task.FromResult(new UserChoice(new UserChoice.GoToPreviousStep()));
                    case NextInput:
                    {
                        var invalidGroups =
                            GuidedInstallerValidation.ValidateStepSelections(installationStep, selectedOptions);

                        if (invalidGroups.Any())
                        {
                            _logger.LogError(
                                "Some groups have invalid selection, please correct them. Invalid groups:\n {InvalidGroups} \n",
                                installationStep.Groups.Select((group, index) => new { group, index })
                                    .Where(x => invalidGroups.Contains(x.group.Id))
                                    .Select(x => $"{x.index + 1} - {x.group.Name} - ${x.group.Type} \n"));
                            continue;
                        }

                        // proceed to the next step
                        return Task.FromResult(new UserChoice(new UserChoice.GoToNextStep(selectedOptions.ToArray())));
                    }
                    default:
                    {
                        var groupIndex = ParseNumericalUserInput(input, installationStep.Groups.Length);
                        if (groupIndex < 0) continue;

                        currentGroup = installationStep.Groups[groupIndex];
                        break;
                    }
                }
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

                        if (!GuidedInstallerValidation.IsValidGroupSelection(currentGroup, selectedOptions))
                        {
                            _logger.LogError(
                                "Selection is invalid for group {GroupIndex}: {GroupType}\n",
                                Array.IndexOf(installationStep.Groups, currentGroup) + 1, currentGroup.Type);
                            continue;
                        }

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
            .Select(group => new object[] { key++, group.Name })
            .Append(installationStep.HasNextStep
                ? TableOfGroupsFooterNextStep
                : TableOfGroupsFooterFinish
            );

        if (installationStep.HasPreviousStep) row = row.Append(TableOfGroupsFooterPreviousStep);
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
                    option.Description ?? string.Empty
                };
            })
            .Append(TableOfOptionsFooterBackToGroupSelection)
            .Append(TableOfOptionsFooterCancel);

        var table = new Table(TableOfOptionsHeaders, row.ToArray(), group.Name);
        Renderer.Render(table);
    }

    private static string RenderOptionState(bool hasSelected, OptionType type)
    {
        Debug.Assert(Enum.IsDefined(typeof(OptionType), type));

        return type switch
        {
            OptionType.Disabled => "Disabled",
            // NOTE (erri120): These are the "initial" states.
            // The user can toggle a pre-selected option, which
            // won't change the type but the "hasSelected" value.
            OptionType.Available or OptionType.PreSelected => hasSelected ? "Selected" : "Off",
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
            // "deselect" all other options in the group that aren't required
            var selectionsToRemove = selectedOptions.Where(x => x.GroupId == currentGroup.Id)
                .Where(x =>
                {
                    var extractedOption = currentGroup.Options.FirstOrDefault(option => option.Id == x.OptionId);

                    // this should never happen, but if we have a selected option that doesn't exist in the group, remove it
                    if (extractedOption is null) return true;

                    return extractedOption.Type is not OptionType.Required;
                })
                .ToArray();

            foreach (var selection in selectionsToRemove)
                selectedOptions.Remove(selection);
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
            // method returns a zero-based index for use as the option index
            // the user inputs a one-based index, as it's easier to understand
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

    /// <inheritdoc/>
    public void Dispose() { }
}
