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
    public GuidedInstallationStep? InstallationStep { get; set; }

    [Reactive]
    public Option? HighlightedOption { get; set; }
    private IGuidedInstallerOptionViewModel? _highlightedOptionViewModel;

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

            this.WhenAnyValue(x => x.Groups)
                .Select(groupVMs => groupVMs
                    .Select(groupVM => groupVM
                        .WhenAnyValue(x => x.HighlightedOption)
                    )
                    .CombineLatest()
                )
                .SubscribeWithErrorLogging(logger: default, observable =>
                {
                    observable
                        .SubscribeWithErrorLogging(logger: default, list =>
                        {
                            var previous = HighlightedOption;
                            var previousVM = _highlightedOptionViewModel;
                            if (previous is null || previousVM is null)
                            {
                                _highlightedOptionViewModel = list.FirstOrDefault(x => x is not null);
                                HighlightedOption = _highlightedOptionViewModel?.Option;
                                return;
                            }

                            var highlightedOptionVMs = list
                                .Where(x => x is not null)
                                .Select(x => x!)
                                .ToArray();

                            var newVM = highlightedOptionVMs.First(x => x.Option.Id != previous.Id);
                            _highlightedOptionViewModel = newVM;
                            HighlightedOption = newVM.Option;

                            foreach (var groupVM in Groups)
                            {
                                if (groupVM.HighlightedOption != previousVM) continue;
                                groupVM.HighlightedOption = null;
                            }
                        })
                        .DisposeWith(disposables);
                })
                .DisposeWith(disposables);
        });
    }
}
