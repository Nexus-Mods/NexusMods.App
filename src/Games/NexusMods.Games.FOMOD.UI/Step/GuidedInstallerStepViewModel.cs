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
    public Percent Progress { get; set; } = Percent.Zero;

    [Reactive]
    public IGuidedInstallerGroupViewModel[] Groups { get; set; } = Array.Empty<IGuidedInstallerGroupViewModel>();

    public IFooterStepperViewModel FooterStepperViewModel { get; } = new FooterStepperViewModel();

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

            // TODO: figure out how to do a "finish" button
            var canGoNext = this.WhenAnyValue(
                x => x.TaskCompletionSource,
                x => x.InstallationStep,
                (tcs, step) => tcs is not null);

            var goToNextStepCommand = ReactiveCommand.Create(() =>
            {
                var selectedOptions = this.GatherSelectedOptions();
                TaskCompletionSource?.TrySetResult(new UserChoice(new UserChoice.GoToNextStep(selectedOptions)));
            }, canGoNext).DisposeWith(disposables);

            var canGoPrev = this.WhenAnyValue(
                x => x.TaskCompletionSource,
                x => x.InstallationStep,
                (tcs, step) => tcs is not null && step is not null && step.HasPreviousStep);

            var goToPrevStepCommand = ReactiveCommand.Create(() =>
            {
                TaskCompletionSource?.TrySetResult(new UserChoice(new UserChoice.GoToPreviousStep()));
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
