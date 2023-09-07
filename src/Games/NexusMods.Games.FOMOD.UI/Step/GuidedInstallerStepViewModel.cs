using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

[UsedImplicitly]
public class GuidedInstallerStepViewModel : AViewModel<IGuidedInstallerStepViewModel>, IGuidedInstallerStepViewModel
{
    [Reactive]
    public string? ModName { get; set; }

    [Reactive]
    public GuidedInstallationStep? InstallationStep { get; set; }

    [Reactive]
    public IGuidedInstallerOptionViewModel? HighlightedOptionViewModel { get; set; }

    [Reactive]
    public TaskCompletionSource<UserChoice>? TaskCompletionSource { get; set; }

    [Reactive]
    public IGuidedInstallerGroupViewModel[] Groups { get; set; } = Array.Empty<IGuidedInstallerGroupViewModel>();

    public ReactiveCommand<Unit, Unit> NextStepCommand { get; set; }
    public ReactiveCommand<Unit, Unit> PreviousStepCommand { get; set; }
    public ReactiveCommand<Unit, Unit> CancelInstallerCommand { get; set; }

    public GuidedInstallerStepViewModel(ILogger<GuidedInstallerStepViewModel> logger)
    {
        // TODO: other validation
        var hasTaskCompletionSource = this
            .WhenAnyValue(vm => vm.TaskCompletionSource)
            .OnUI()
            .Select(tcs => tcs is not null);

        NextStepCommand = ReactiveCommand.Create(() =>
        {
            var selectedOptions = this.GatherSelectedOptions();
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

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.InstallationStep)
                .WhereNotNull()
                .SubscribeWithErrorLogging(logger, installationStep =>
                {
                    HighlightedOptionViewModel = null;
                    Groups = installationStep.Groups
                        .Select(group => (IGuidedInstallerGroupViewModel)new GuidedInstallerGroupViewModel(group))
                        .ToArray();
                })
                .DisposeWith(disposables);

            this.SetupCrossGroupOptionHighlighting(disposables);
        });
    }
}
