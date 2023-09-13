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

    [Reactive]
    public bool HasValidSelections { get; set; }

    public GuidedInstallerStepViewModel(ILogger<GuidedInstallerStepViewModel> logger)
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

                    // highlight the first option when the user changes steps
                    var group = Groups.First();
                    group.HighlightedOption = group.Options.First();
                    HighlightedOptionViewModel = group.HighlightedOption;
                })
                .DisposeWith(disposables);

            this.SetupCrossGroupOptionHighlighting(disposables);
            this.SetupHighlightedOption(_highlightedOptionImageSubject, disposables);

            this.WhenAnyValue(x => x.Groups)
                .Select(groupVMs => groupVMs
                    .Select(groupVM => groupVM
                        .WhenAnyValue(x => x.HasValidSelection)
                    )
                    .CombineLatest()
                    .Select(list => list.All(isValid => isValid))
                )
                .SubscribeWithErrorLogging(logger: default, observable =>
                {
                    observable
                        .SubscribeWithErrorLogging(logger: default, allValid => HasValidSelections = allValid)
                        .DisposeWith(disposables);
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
                    var selectedOptions = this.GatherSelectedOptions();
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
