using FomodInstaller.Interface;
using FomodInstaller.Interface.ui;
using Microsoft.Extensions.Logging;
using NexusMods.Common.UserInput;

namespace NexusMods.Games.FOMOD.CoreDelegates;

public class UiDelegates : IUIDelegates
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
    private readonly IOptionSelector _optionSelector;

    private SelectOptions _selectOptions = DummySelectOptions;
    private ContinueToNextStep _continueToNextStep = DummyContinueToNextStep;
    private CancelInstaller _cancelInstaller = DummyCancelInstaller;

    public UiDelegates(ILogger<UiDelegates> logger, IOptionSelector optionSelector)
    {
        _logger = logger;
        _optionSelector = optionSelector;
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

        _optionSelector.SetupSelector(moduleName ?? string.Empty);
    }

    public void EndDialog()
    {
        _selectOptions = DummySelectOptions;
        _continueToNextStep = DummyContinueToNextStep;

        _optionSelector.CleanupSelector();
    }

    public void ReportError(
        string title,
        string message,
        string details)
    {
        _logger.LogError("Reporting an error: {Title}: {Message}\n{Details}", title, message, details);
    }

    public void UpdateState(InstallerStep[] installSteps, int currentStepId)
    {
        var currentStep = installSteps[currentStepId];
        var choices = ToChoices(currentStep.optionalFileGroups).ToArray();

        _optionSelector
            .RequestMultipleChoices(choices)
            .ContinueWith(task =>
            {
                var result = task.Result;
                _logger.LogDebug("Status: {TaskStatus}", task.Status);

                // TODO: figure out how to go backwards. Our internal API doesn't support this concept.
                if (result is null)
                {
                    // Result is null, the user wants to cancel installation.
                    _cancelInstaller();
                    return;
                }

                // NOTE(erri120): This _selectOptions delegate we got from the executor is
                // fucking weird. It only accepts the selected options from a single group.
                // Vortex: https://github.com/Nexus-Mods/Vortex/blob/82e8ad3e051ab4ad41df43d11803ee43d399a85f/src/extensions/installer_fomod/views/InstallerDialog.tsx#L450-L461
                // This is "supposed" to be called whenever the user clicks on a button in the UI.
                // However, this explicit relationship doesn't work in our case and is generally
                // pretty stupid. As such, we get the "final" result from the implementation
                // and push those all at once to the executor.

                foreach (var kv in result)
                {
                    var (selectedGroupId, selectedOptionIds) = kv;
                    _selectOptions(currentStepId, selectedGroupId, selectedOptionIds);
                }

                // We need to explicitly call this, since our implementations
                // return from RequestMultipleChoices when the user wants to go
                // to the next step. Once again, the API is ass.
                _continueToNextStep(forward: true, currentStepId);
            });
    }

    private static IEnumerable<ChoiceGroup<int, int>> ToChoices(GroupList groups)
    {
        return groups.group.Select(group => new ChoiceGroup<int, int>
        {
            Id = group.id,
            Type = ConvertChoiceType(group.type),
            Query = group.name,
            Options = ToOptions(group.options).ToArray(),
        });
    }

    private static IEnumerable<Option<int>> ToOptions(IEnumerable<Option> options)
    {
        return options.Select(option => new Option<int>
        {
            Id = option.id,
            Name = option.name,
            Description = option.description,
            ImageUrl = option.image != null ? AssetUrl.From(option.image) : null,
            Type = MakeOptionState(option),
        });
    }

    private static OptionState MakeOptionState(Option option)
    {
        var state = option.type switch
        {
            "Required" => OptionState.Required,
            "NotUsable" => OptionState.Disabled,
            "Recommended" => OptionState.Selected,
            _ => OptionState.Available
        };

        if (state == OptionState.Available && option.selected) state = OptionState.Selected;
        return state;
    }

    private static ChoiceType ConvertChoiceType(string input)
    {
        return input switch
        {
            "SelectAtLeastOne" => ChoiceType.AtLeastOne,
            "SelectAtMostOne" => ChoiceType.AtMostOne,
            "SelectExactlyOne" => ChoiceType.ExactlyOne,
            _ => ChoiceType.Any
        };
    }
}
