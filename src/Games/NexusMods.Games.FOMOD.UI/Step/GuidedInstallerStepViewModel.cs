using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Media;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using NexusMods.DataModel.RateLimiting;
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
    public string? HighlightedOptionDescription { get; set; }

    private readonly Subject<IImage> _highlightedOptionImageSubject = new();
    public IObservable<IImage> HighlightedOptionImageObservable => _highlightedOptionImageSubject;

    [Reactive]
    public TaskCompletionSource<UserChoice>? TaskCompletionSource { get; set; }

    [Reactive]
    public IGuidedInstallerGroupViewModel[] Groups { get; set; } = Array.Empty<IGuidedInstallerGroupViewModel>();

    [Reactive]
    public Percent Progress { get; set; } = Percent.Zero;

    [Reactive]
    public bool ShowInstallationCompleteScreen { get; set; }

    public IFooterStepperViewModel FooterStepperViewModel { get; } = new FooterStepperViewModel();

    private Percent _previousProgress = Percent.Zero;

    public GuidedInstallerStepViewModel(ILogger<GuidedInstallerStepViewModel> logger)
    {
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
            this.SetupHighlightedOption(_highlightedOptionImageSubject, disposables);

            var canGoNext = this
                .WhenAnyValue(
                    x => x.TaskCompletionSource,
                    x => x.InstallationStep,
                    (tcs, step) => tcs is not null && step is not null);

            var goToNextStepCommand = ReactiveCommand.Create(() =>
            {
                var selectedOptions = this.GatherSelectedOptions();
                var failedGroupIds = GuidedInstallerValidation.ValidateStepSelections(InstallationStep!, selectedOptions);

                if (failedGroupIds.Length != 0)
                {
                    for (var i = 0; i < failedGroupIds.Length; i++)
                    {
                        var tmp = i;
                        var groupVM = Groups.FirstOrDefault(x => failedGroupIds[tmp] == x.Group.Id);

                        if (groupVM is null) continue;
                        groupVM.HasValidSelection = false;
                    }

                    return;
                }

                // NOTE(erri120): On the last step, we don't set the result but instead show a "installation complete"-screen.
                if (InstallationStep!.HasNextStep || ShowInstallationCompleteScreen)
                {
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
                (tcs, step, showInstallationCompleteScreen) => showInstallationCompleteScreen || (tcs is not null && step is not null && step.HasPreviousStep));

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
                .SubscribeWithErrorLogging(logger: default, progress =>
                {
                    FooterStepperViewModel.Progress = progress;
                });
        });
    }
}
