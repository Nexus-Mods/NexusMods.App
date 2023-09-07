using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using NexusMods.Common.GuidedInstaller.ValueObjects;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerStepDesignViewModel : AViewModel<IGuidedInstallerStepViewModel>, IGuidedInstallerStepViewModel
{
    public GuidedInstallationStep? InstallationStep { get; set; }

    [Reactive]
    public Option? HighlightedOption { get; set; }
    private IGuidedInstallerOptionViewModel? _highlightedOptionViewModel;

    public TaskCompletionSource<UserChoice>? TaskCompletionSource { get; set; }

    [Reactive]
    public IGuidedInstallerGroupViewModel[] Groups { get; set; }

    public ReactiveCommand<Unit, Unit> NextStepCommand { get; set; } = Initializers.ReactiveCommandUnitUnit;
    public ReactiveCommand<Unit, Unit> PreviousStepCommand { get; set; } = Initializers.ReactiveCommandUnitUnit;
    public ReactiveCommand<Unit, Unit> CancelInstallerCommand { get; set; } = Initializers.ReactiveCommandUnitUnit;

    public GuidedInstallerStepDesignViewModel()
    {
        var step = SetupInstallationStep();

        InstallationStep = step;
        Groups = step.Groups
            .Select(group => (IGuidedInstallerGroupViewModel)new GuidedInstallerGroupDesignViewModel(group))
            .ToArray();

        this.WhenActivated(disposables =>
        {
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

    private static GuidedInstallationStep SetupInstallationStep()
    {
        return new GuidedInstallationStep
        {
            Id = StepId.From(Guid.NewGuid()),
            Name = "Test Step",
            HasNextStep = false,
            HasPreviousStep = false,
            Groups = new[]
            {
                new OptionGroup
                {
                    Id = GroupId.From(Guid.NewGuid()),
                    Name = "Group 1",
                    Type = OptionGroupType.Any,
                    Options = GuidedInstallerGroupDesignViewModel.GenerateOptions()
                },
                new OptionGroup
                {
                    Id = GroupId.From(Guid.NewGuid()),
                    Name = "Group 2",
                    Type = OptionGroupType.Any,
                    Options = GuidedInstallerGroupDesignViewModel.GenerateOptions()
                },
            }
        };
    }
}
