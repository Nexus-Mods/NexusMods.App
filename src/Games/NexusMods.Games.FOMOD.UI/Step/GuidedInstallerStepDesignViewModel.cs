using System.Collections.ObjectModel;
using System.Reactive;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using NexusMods.Common.GuidedInstaller.ValueObjects;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerStepDesignViewModel : AViewModel<IGuidedInstallerStepViewModel>, IGuidedInstallerStepViewModel
{
    public GuidedInstallationStep? InstallationStep { get; set; }
    public TaskCompletionSource<UserChoice>? TaskCompletionSource { get; set; }
    public ReadOnlyObservableCollection<IGuidedInstallerGroupViewModel> Groups { get; set; }

    public ReactiveCommand<Unit, Unit> NextStepCommand { get; set; } = Initializers.ReactiveCommandUnitUnit;
    public ReactiveCommand<Unit, Unit> PreviousStepCommand { get; set; } = Initializers.ReactiveCommandUnitUnit;
    public ReactiveCommand<Unit, Unit> CancelInstallerCommand { get; set; } = Initializers.ReactiveCommandUnitUnit;

    public GuidedInstallerStepDesignViewModel()
    {
        var step = SetupInstallationStep();

        InstallationStep = step;
        Groups = new ReadOnlyObservableCollection<IGuidedInstallerGroupViewModel>(new
            ObservableCollection<IGuidedInstallerGroupViewModel>(
                step.Groups.Select(group => new GuidedInstallerGroupDesignViewModel(group))
            )
        );
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
                    Description = "Group 1",
                    Type = OptionGroupType.Any,
                    Options = new[]
                    {
                        new Option
                        {
                            Id = OptionId.From(Guid.NewGuid()),
                            Name = "Option 1",
                            Type = OptionType.Available,
                            Description = "This is an option",
                        }
                    }
                }
            }
        };
    }
}
