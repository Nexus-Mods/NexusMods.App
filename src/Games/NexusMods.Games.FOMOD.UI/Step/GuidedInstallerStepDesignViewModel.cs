using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Media;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using NexusMods.Common.GuidedInstaller.ValueObjects;
using NexusMods.DataModel.RateLimiting;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerStepDesignViewModel : AViewModel<IGuidedInstallerStepViewModel>, IGuidedInstallerStepViewModel
{
    public string? ModName { get; set; } = "Example Mod";

    public GuidedInstallationStep? InstallationStep { get; set; }

    [Reactive]
    public IGuidedInstallerOptionViewModel? HighlightedOptionViewModel { get; set; }

    [Reactive]
    public string? HighlightedOptionDescription { get; set; }

    private readonly Subject<IImage> _highlightedOptionImageSubject = new();
    public IObservable<IImage> HighlightedOptionImageObservable => _highlightedOptionImageSubject;

    public TaskCompletionSource<UserChoice>? TaskCompletionSource { get; set; }

    public Percent Progress { get; set; } = Percent.CreateClamped(4, 10);

    [Reactive]
    public IGuidedInstallerGroupViewModel[] Groups { get; set; }

    public ReactiveCommand<Unit, Unit> NextStepCommand { get; set; } = Initializers.ReactiveCommandUnitUnit;
    public ReactiveCommand<Unit, Unit> PreviousStepCommand { get; set; }
    public ReactiveCommand<Unit, Unit> CancelInstallerCommand { get; set; } = Initializers.ReactiveCommandUnitUnit;

    public GuidedInstallerStepDesignViewModel()
    {
        var step = SetupInstallationStep();

        InstallationStep = step;
        Groups = step.Groups
            .Select(group => (IGuidedInstallerGroupViewModel)new GuidedInstallerGroupDesignViewModel(group))
            .ToArray();

        PreviousStepCommand = ReactiveCommand.Create(() => { }, Observable.Return(false));

        this.WhenActivated(disposables =>
        {
            this.SetupCrossGroupOptionHighlighting(disposables);
            this.SetupHighlightedOption(_highlightedOptionImageSubject, disposables);
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
