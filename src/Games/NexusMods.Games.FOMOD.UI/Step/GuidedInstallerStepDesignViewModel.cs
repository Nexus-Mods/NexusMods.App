using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using NexusMods.Common.GuidedInstaller.ValueObjects;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerStepDesignViewModel : AViewModel<IGuidedInstallerStepViewModel>, IGuidedInstallerStepViewModel
{
    public GuidedInstallationStep? InstallationStep { get; set; }
    public Option? HighlightedOption { get; set; }
    public TaskCompletionSource<UserChoice>? TaskCompletionSource { get; set; }
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
            Groups
                .Select(groupVM => groupVM
                    .WhenAnyValue(x => x.HighlightedOption))
                .CombineLatest()
                .SubscribeWithErrorLogging(logger: default, options =>
                {
                    HighlightedOption = options.FirstOrDefault();
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
                    Options = new[]
                    {
                        new Option
                        {
                            Id = OptionId.From(Guid.NewGuid()),
                            Name = "Available Option",
                            Type = OptionType.Available,
                            Description = "This option is available",
                        },
                        new Option
                        {
                            Id = OptionId.From(Guid.NewGuid()),
                            Name = "Pre-selected Option",
                            Type = OptionType.PreSelected,
                            Description = "This option is pre-selected"
                        },
                        new Option
                        {
                            Id = OptionId.From(Guid.NewGuid()),
                            Name = "Required Option",
                            Type = OptionType.Required,
                            Description = "This option is required"
                        },
                        new Option
                        {
                            Id = OptionId.From(Guid.NewGuid()),
                            Name = "Disabled Option",
                            Type = OptionType.Disabled,
                            Description = "This option is disabled"
                        },
                    }
                }
            }
        };
    }
}
