using System.Reactive;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using NexusMods.Common.GuidedInstaller.ValueObjects;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerStepDesignViewModel : AViewModel<IGuidedInstallerStepViewModel>, IGuidedInstallerStepViewModel
{
    public string? ModName { get; set; } = "Example Mod";

    public GuidedInstallationStep? InstallationStep { get; set; }

    [Reactive]
    public IGuidedInstallerOptionViewModel? HighlightedOptionViewModel { get; set; }

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

        this.WhenActivated(this.SetupCrossGroupOptionHighlighting);
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
                    Name = "Group 1 (Any)",
                    Type = OptionGroupType.Any,
                    Options = GuidedInstallerGroupDesignViewModel.GenerateAllOptionTypes()
                },
                new OptionGroup
                {
                    Id = GroupId.From(Guid.NewGuid()),
                    Name = "Group 2 (Exactly One)",
                    Type = OptionGroupType.ExactlyOne,
                    Options = GuidedInstallerGroupDesignViewModel.GenerateOptions()
                },
                new OptionGroup
                {
                    Id = GroupId.From(Guid.NewGuid()),
                    Name = "Group 3 (At Most One)",
                    Type = OptionGroupType.AtMostOne,
                    Options = GuidedInstallerGroupDesignViewModel.GenerateOptions()
                },
                new OptionGroup
                {
                    Id = GroupId.From(Guid.NewGuid()),
                    Name = "Group 4 (At Least One)",
                    Type = OptionGroupType.AtLeastOne,
                    Options = GuidedInstallerGroupDesignViewModel.GenerateOptions()
                },
            }
        };
    }
}
