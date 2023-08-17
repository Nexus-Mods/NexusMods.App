using System.Reactive;
using System.Reactive.Linq;
using JetBrains.Annotations;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

[UsedImplicitly]
public class GuidedInstallerStepViewModel : AViewModel<IGuidedInstallerStepViewModel>, IGuidedInstallerStepViewModel
{
    [Reactive]
    public GuidedInstallationStep? InstallationStep { get; set; }

    [Reactive]
    public TaskCompletionSource<UserChoice>? TaskCompletionSource { get; set; }

    public ReactiveCommand<Unit, Unit> NextStepCommand { get; set; }
    public ReactiveCommand<Unit, Unit> PreviousStepCommand { get; set; }
    public ReactiveCommand<Unit, Unit> CancelInstallerCommand { get; set; }

    public GuidedInstallerStepViewModel()
    {
        // TODO: other validation
        var hasTaskCompletionSource = this
            .WhenAnyValue(vm => vm.TaskCompletionSource)
            .OnUI()
            .Select(tcs => tcs is not null);

        NextStepCommand = ReactiveCommand.Create(() =>
        {
            // TODO: set result
            var selectedOptions = Array.Empty<SelectedOption>();
            TaskCompletionSource?.TrySetResult(new UserChoice(new UserChoice.GoToNextStep(selectedOptions)));
        }, hasTaskCompletionSource);

        PreviousStepCommand = ReactiveCommand.Create(() =>
        {
            TaskCompletionSource?.TrySetResult(new UserChoice(new UserChoice.GoToPreviousStep()));
        }, hasTaskCompletionSource);

        CancelInstallerCommand = ReactiveCommand.Create(() =>
        {
            TaskCompletionSource?.TrySetResult(new UserChoice(new UserChoice.CancelInstallation()));
        }, hasTaskCompletionSource);
    }
}
