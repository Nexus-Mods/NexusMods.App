using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.Common.GuidedInstaller;
using NexusMods.Common.GuidedInstaller.ValueObjects;

namespace NexusMods.Games.FOMOD.CoreDelegates;

public sealed class UiDelegates : FomodInstaller.Interface.ui.IUIDelegates, IDisposable
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
    /// should exit.
    /// </summary>
    private delegate void CancelInstaller();
    private static void DummyCancelInstaller() { }

    private readonly ILogger<UiDelegates> _logger;
    private readonly IGuidedInstaller _guidedInstaller;

    private SelectOptions _selectOptions = DummySelectOptions;
    private ContinueToNextStep _continueToNextStep = DummyContinueToNextStep;
    private CancelInstaller _cancelInstaller = DummyCancelInstaller;

    private readonly SemaphoreSlim _semaphoreSlim = new (1, 1);
    private readonly EventWaitHandle _waitHandle = new ManualResetEvent(initialState: false);
    private long _taskWaitingState;

    private const long Ready = 0;
    private const long WaitingForCallback = 1;

    public UiDelegates(ILogger<UiDelegates> logger, IGuidedInstaller guidedInstaller)
    {
        _logger = logger;
        _guidedInstaller = guidedInstaller;
    }

    public void StartDialog(
        string? moduleName,
        FomodInstaller.Interface.HeaderImage image,
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
        // NOTE(erri120): The FOMOD library we're using can send us stupid inputs.
        if (currentStepId < 0 || currentStepId >= installSteps.Length) return;

        // NOTE(erri120): This fuckery is explained further below when we call _selectOptions()
        if (Interlocked.Read(ref _taskWaitingState) == WaitingForCallback)
        {
            if (!_waitHandle.Set())
            {
                _logger.LogWarning("Unable to signal completion!");
            }

            return;
        }

        // NOTE(erri120): The FOMOD library we're using was designed for threading in JavaScript.
        // A semaphore is required because the library can spawn multiple tasks on different threads
        // that will call this method multiple times. This can lead to double-state and it's
        // just a complete mess. This is what you get when you write a .NET library for JavaScript...
        using var waiter = _semaphoreSlim.CustomWait(TimeSpan.Zero);
        if (!waiter.HasEntered) return;

        var groupIdMappings = new List<KeyValuePair<int, GroupId>>();
        var optionIdMappings = new List<KeyValuePair<int, OptionId>>();

        var guidedInstallationStep = ToGuidedInstallationStep(
            installSteps,
            currentStepId,
            groupIdMappings,
            optionIdMappings
        );

        _guidedInstaller
            .RequestUserChoice(guidedInstallationStep, new CancellationToken())
            .ContinueWith(task =>
            {
                var result = task.Result;

                if (result.IsCancelInstallation)
                {
                    _cancelInstaller();

                    // NOTE(erri120): We have to manually call this method.
                    EndDialog();
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

                        // NOTE(erri120): Once again, the FOMOD library we're using is complete ass and expects
                        // to be used in a JavaScript environment. However, this isn't JavaScript this is C#.
                        // When calling _selectOptions, the library spawns a new Task that runs in the background.
                        // This means that after calling _selectOptions, the state hasn't been updated YET.
                        // Inside _selectOptions, the library wants to get the next step, however, if we
                        // call _continueToNextStep, then the next step variable has already been updated.
                        // The library doesn't do any checks to prevent this and can throw an exception
                        // if we're at the last step and continue. In that case, the step variable will be set to -1
                        // and steps[-1] will throw an exception.
                        // As such, we're required to use our own synchronization. Thankfully, we can abuse the stupid
                        // behavior of the library. As explained earlier, the library expects us to call _selectOptions
                        // when the user clicks on a button. The method _selectOptions will call us back, once it has
                        // updated its internal state. This behavior allows us to use an EventWaitHandle to "wait" for
                        // the library to call us back.
                        // We're also using CAS to set our state into "waiting" mode.
                        if (Interlocked.CompareExchange(ref _taskWaitingState, WaitingForCallback, Ready) != Ready)
                            _logger.LogWarning("Unable to CAS!");

                        _selectOptions(currentStepId, selectedGroupId, selectedOptionIds);
                        _waitHandle.WaitOne(TimeSpan.FromMilliseconds(200), exitContext: false);
                        _waitHandle.Reset();

                        if (Interlocked.CompareExchange(ref _taskWaitingState, Ready, WaitingForCallback) != WaitingForCallback)
                            _logger.LogWarning("Unable to CAS!");
                    }

                    // NOTE(erri120): We need to explicitly call this, since our implementations
                    // return from RequestMultipleChoices when the user wants to go
                    // to the next step. Once again, the API is ass.
                    _continueToNextStep(forward: true, currentStepId);
                }
            });
    }

    private static GuidedInstallationStep ToGuidedInstallationStep(
        IList<FomodInstaller.Interface.ui.InstallerStep> installSteps,
        int currentStepId,
        ICollection<KeyValuePair<int, GroupId>> groupIdMapping,
        ICollection<KeyValuePair<int, OptionId>> optionIdMappings)
    {
        var groups = installSteps[currentStepId].optionalFileGroups;

        var stepGroups = groups.group.Select(group =>
        {
            var groupId = GroupId.From(Guid.NewGuid());
            groupIdMapping.Add(new KeyValuePair<int, GroupId>(group.id, groupId));

            return new OptionGroup
            {
                Id = groupId,
                Type = ConvertOptionGroupType(group.type),
                Name = group.name,
                Options = ToOptions(group.options, optionIdMappings).ToArray(),
            };
        });

        return new GuidedInstallationStep
        {
            Id = StepId.From(Guid.NewGuid()),
            Name = installSteps[currentStepId].name ?? string.Empty,
            Groups = stepGroups.ToArray(),
            HasPreviousStep = currentStepId != 0,
            HasNextStep = currentStepId != installSteps.Count - 1,
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
                Type = MakeOptionType(option),
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

    public void Dispose()
    {
        _semaphoreSlim.Dispose();
        _waitHandle.Dispose();
    }
}
