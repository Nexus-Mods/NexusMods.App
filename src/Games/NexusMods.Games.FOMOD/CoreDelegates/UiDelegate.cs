using FomodInstaller.Interface;
using Microsoft.Extensions.Logging;
using NexusMods.Common.GuidedInstaller;
using NexusMods.Common.GuidedInstaller.ValueObjects;

namespace NexusMods.Games.FOMOD.CoreDelegates;

public class UiDelegates : FomodInstaller.Interface.ui.IUIDelegates
{
    /// <summary>
    /// Created by the executor, this delegate notifies the executor about which options the
    /// user selected.
    /// </summary>
    /// <param name="stepId">
    /// The ID of the step where the user selected the options.
    /// This is most likely going to be the current step.
    /// </param>
    /// <param name="selectedGroupId">The ID of the selected group.</param>
    /// <param name="selectedOptionIds">The IDs of the selected options.</param>
    private delegate void SelectOptions(int stepId, int selectedGroupId, int[] selectedOptionIds);
    private static void DummySelectOptions(int stepId, int selectedGroupId, int[] selectedOptionIds) { }

    /// <summary>
    /// Created by the executor, this delegate notifies the executor that the installer should
    /// proceed to the next or previous step.
    /// </summary>
    /// <param name="forward">Whether to move forward or backwards in the installer.</param>
    /// <param name="currentStepId">The ID of the current step.</param>
    private delegate void ContinueToNextStep(bool forward, int currentStepId);
    private static void DummyContinueToNextStep(bool forward, int currentStepId) { }

    /// <summary>
    /// Created by the executor, this delegate notifies the executor that the installer
    /// should exist.
    /// </summary>
    private delegate void CancelInstaller();
    private static void DummyCancelInstaller() { }

    private readonly ILogger<UiDelegates> _logger;
    private readonly IGuidedInstaller _guidedInstaller;

    private SelectOptions _selectOptions = DummySelectOptions;
    private ContinueToNextStep _continueToNextStep = DummyContinueToNextStep;
    private CancelInstaller _cancelInstaller = DummyCancelInstaller;

    public UiDelegates(ILogger<UiDelegates> logger, IGuidedInstaller guidedInstaller)
    {
        _logger = logger;
        _guidedInstaller = guidedInstaller;
    }

    public void StartDialog(
        string? moduleName,
        HeaderImage image,
        Action<int, int, int[]> select,
        Action<bool, int> cont,
        Action cancel)
    {
        _selectOptions = new SelectOptions(select);
        _continueToNextStep = new ContinueToNextStep(cont);
        _cancelInstaller = new CancelInstaller(cancel);

        _guidedInstaller.SetupInstaller(moduleName ?? "FOMOD Installer");
    }

    public void EndDialog()
    {
        _selectOptions = DummySelectOptions;
        _continueToNextStep = DummyContinueToNextStep;

        _guidedInstaller.CleanupInstaller();
    }

    public void ReportError(
        string title,
        string message,
        string details)
    {
        _logger.LogError("Reporting an error: {Title}: {Message}\n{Details}", title, message, details);
    }

    public void UpdateState(FomodInstaller.Interface.ui.InstallerStep[] installSteps, int currentStepId)
    {
        var currentStep = installSteps[currentStepId];
        var groupIdMappings = new List<KeyValuePair<int, GroupId>>();
        var optionIdMappings = new List<KeyValuePair<int, OptionId>>();

        var guidedInstallationStep = ToGuidedInstallationStep(
            currentStep.optionalFileGroups,
            groupIdMappings,
            optionIdMappings
        );

        _guidedInstaller
            .RequestUserChoice(guidedInstallationStep, new CancellationToken())
            .ContinueWith(task =>
            {
                _logger.LogDebug("Status: {TaskStatus}", task.Status);
                var result = task.Result;

                if (result.IsCancelInstallation)
                {
                    // TODO: verify that this calls EndDialog
                    _cancelInstaller();
                } else if (result.IsGoToPreviousStep)
                {
                    _continueToNextStep(forward: false, currentStepId);
                } else if (result.IsGoToNextStep)
                {
                    var selectedOptions = result.AsT2.SelectedOptions;

                    // NOTE(erri120): This _selectOptions delegate we got from the executor is
                    // fucking weird. It only accepts the selected options from a single group.
                    // Vortex: https://github.com/Nexus-Mods/Vortex/blob/82e8ad3e051ab4ad41df43d11803ee43d399a85f/src/extensions/installer_fomod/views/InstallerDialog.tsx#L450-L461
                    // This is "supposed" to be called whenever the user clicks on a button in the UI.
                    // However, this explicit relationship doesn't work in our case and is generally
                    // pretty stupid. As such, we get the "final" result from the implementation
                    // and push those all at once to the executor.

                    var groupings = selectedOptions.GroupBy(x => x.GroupId);

                    foreach (var grouping in groupings)
                    {
                        var selectedGroupId = groupIdMappings.First(kv => kv.Value == grouping.Key).Key;
                        var selectedOptionIds = grouping
                            .Select(x => optionIdMappings.FirstOrDefault(kv => kv.Value == x.OptionId).Key)
                            .ToArray();

                        _selectOptions(currentStepId, selectedGroupId, selectedOptionIds);
                    }

                    // We need to explicitly call this, since our implementations
                    // return from RequestMultipleChoices when the user wants to go
                    // to the next step. Once again, the API is ass.
                    _continueToNextStep(forward: true, currentStepId);
                }
            });
    }

    private static GuidedInstallationStep ToGuidedInstallationStep(
        FomodInstaller.Interface.ui.GroupList groups,
        ICollection<KeyValuePair<int, GroupId>> groupIdMapping,
        ICollection<KeyValuePair<int, OptionId>> optionIdMappings)
    {
        var stepGroups = groups.group.Select(group =>
        {
            var groupId = GroupId.From(Guid.NewGuid());
            groupIdMapping.Add(new KeyValuePair<int, GroupId>(group.id, groupId));

            return new OptionGroup
            {
                Id = groupId,
                OptionGroupType = ConvertOptionGroupType(group.type),
                Description = group.name,
                Options = ToOptions(group.options, optionIdMappings).ToArray(),
            };
        });

        return new GuidedInstallationStep
        {
            Id = StepId.From(Guid.NewGuid()),
            Groups = stepGroups.ToArray()
        };
    }

    private static IEnumerable<Option> ToOptions(
        IEnumerable<FomodInstaller.Interface.ui.Option> options,
        ICollection<KeyValuePair<int, OptionId>> optionIdMappings)
    {
        return options.Select(option =>
        {
            var optionId = OptionId.From(Guid.NewGuid());
            optionIdMappings.Add(new KeyValuePair<int, OptionId>(option.id, optionId));

            return new Option
            {
                Id = optionId,
                Name = option.name,
                Description = option.description,
                ImageUrl = option.image != null ? AssetUrl.From(option.image) : null,
                OptionType = MakeOptionType(option),
            };
        });
    }

    private static OptionType MakeOptionType(FomodInstaller.Interface.ui.Option option)
    {
        var state = option.type switch
        {
            "Required" => OptionType.Required,
            "NotUsable" => OptionType.Disabled,
            "Recommended" => OptionType.PreSelected,
            _ => OptionType.Available
        };

        return state;
    }

    private static OptionGroupType ConvertOptionGroupType(string input)
    {
        return input switch
        {
            "SelectAtLeastOne" => OptionGroupType.AtLeastOne,
            "SelectAtMostOne" => OptionGroupType.AtMostOne,
            "SelectExactlyOne" => OptionGroupType.ExactlyOne,
            _ => OptionGroupType.Any
        };
    }
}
