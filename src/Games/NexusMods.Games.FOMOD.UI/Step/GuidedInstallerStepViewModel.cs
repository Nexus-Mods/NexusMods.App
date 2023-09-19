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

            var canGoNext = this
                .WhenAnyValue(
                    x => x.TaskCompletionSource,
                    x => x.InstallationStep,
                    x => x.HasValidSelections,
                    (tcs, step, hasValidSelections) => tcs is not null && step is not null && hasValidSelections);

            var goToNextStepCommand = ReactiveCommand.Create(() =>
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
            }, canGoNext).DisposeWith(disposables);

            var canGoPrev = this.WhenAnyValue(
                x => x.TaskCompletionSource,
                x => x.InstallationStep,
                x => x.ShowInstallationCompleteScreen,
                (tcs, step, showInstallationCompleteScreen) => showInstallationCompleteScreen ||
                                                               (tcs is not null && step is not null &&
                                                                step.HasPreviousStep));

            var goToPrevStepCommand = ReactiveCommand.Create(() =>
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
            }, canGoPrev).DisposeWith(disposables);

            FooterStepperViewModel.GoToNextCommand = goToNextStepCommand;
            FooterStepperViewModel.GoToPrevCommand = goToPrevStepCommand;

            this.WhenAnyValue(x => x.Progress)
                .SubscribeWithErrorLogging(logger: default, progress => { FooterStepperViewModel.Progress = progress; })
                .DisposeWith(disposables);
        });
    }
}
