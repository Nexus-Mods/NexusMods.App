using System.Reactive.Disposables;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using NexusMods.DataModel.RateLimiting;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

[UsedImplicitly]
public sealed class GuidedInstallerStepViewModel : AGuidedInstallerStepViewModel
{
    public override IFooterStepperViewModel FooterStepperViewModel { get; } = new FooterStepperViewModel();

    private Percent _previousProgress = Percent.Zero;

    public GuidedInstallerStepViewModel(
        ILogger<GuidedInstallerStepViewModel> logger,
        IImageCache imageCache) : base(imageCache)
    {
        var goToNextCommand = ReactiveCommand.Create(() =>
        {
            // NOTE(erri120): On the last step, we don't set the result but instead show a "installation complete"-screen.
            if (InstallationStep!.HasNextStep || ShowInstallationCompleteScreen)
            {
                var selectedOptions = GatherSelectedOptions();
                TaskCompletionSource?.TrySetResult(new UserChoice(new UserChoice.GoToNextStep(selectedOptions)));
            }
            else
            {
                _previousProgress = Progress;
                Progress = Percent.One;
                ShowInstallationCompleteScreen = true;
            }
        });

        var goToPrevCommand = ReactiveCommand.Create(() =>
        {
            if (ShowInstallationCompleteScreen)
            {
                ShowInstallationCompleteScreen = false;
                Progress = _previousProgress;
            }
            else
            {
                TaskCompletionSource?.TrySetResult(new UserChoice(new UserChoice.GoToPreviousStep()));
            }
        });

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.InstallationStep)
                .WhereNotNull()
                .SubscribeWithErrorLogging(logger, installationStep =>
                {
                    Groups = installationStep.Groups
                        .Select(group => (IGuidedInstallerGroupViewModel)new GuidedInstallerGroupViewModel(group))
                        .ToArray();
                })
                .DisposeWith(disposables);

            // CanGoNext
            this.WhenAnyValue(
                vm => vm.TaskCompletionSource,
                vm => vm.InstallationStep,
                vm => vm.HasValidSelections,
                (tcs, step, hasValidSelections) => tcs is not null && step is not null && hasValidSelections)
                .BindTo(this, vm => vm.FooterStepperViewModel.CanGoNext)
                .DisposeWith(disposables);

            // CanGoPrev
            this.WhenAnyValue(
                vm => vm.TaskCompletionSource,
                vm => vm.InstallationStep,
                vm => vm.ShowInstallationCompleteScreen,
                (tcs, step, showInstallationCompleteScreen) => showInstallationCompleteScreen || (tcs is not null && step is not null && step.HasPreviousStep))
                .BindTo(this, vm => vm.FooterStepperViewModel.CanGoPrev)
                .DisposeWith(disposables);

            // GoToNext
            this.WhenAnyObservable(vm => vm.FooterStepperViewModel.GoToNextCommand)
                .InvokeCommand(goToNextCommand)
                .DisposeWith(disposables);

            // GoToPrev
            this.WhenAnyObservable(vm => vm.FooterStepperViewModel.GoToPrevCommand)
                .InvokeCommand(goToPrevCommand)
                .DisposeWith(disposables);

            this.WhenAnyValue(vm => vm.Progress)
                .BindTo(this, vm => vm.FooterStepperViewModel.Progress)
                .DisposeWith(disposables);
        });
    }
}
