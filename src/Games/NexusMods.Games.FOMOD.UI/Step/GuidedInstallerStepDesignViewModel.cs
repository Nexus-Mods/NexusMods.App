using System.Reactive.Disposables;
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

    [Reactive]
    public IGuidedInstallerGroupViewModel[] Groups { get; set; }

    public Percent Progress { get; set; }

    public IFooterStepperViewModel FooterStepperViewModel { get; }

    [Reactive]
    public bool ShowInstallationCompleteScreen { get; set; }

    public GuidedInstallerStepDesignViewModel()
    {
        var step = SetupInstallationStep();

        InstallationStep = step;
        Groups = step.Groups
            .Select(group =>
            {
                var vm = (IGuidedInstallerGroupViewModel)new GuidedInstallerGroupDesignViewModel(group);
                if (group.Type == OptionGroupType.AtLeastOne) vm.HasValidSelection = false;
                return vm;
            })
            .ToArray();

        FooterStepperViewModel = new FooterStepperDesignViewModel(Percent.Zero);

        this.WhenActivated(disposables =>
        {
            this.SetupCrossGroupOptionHighlighting(disposables);
            this.SetupHighlightedOption(_highlightedOptionImageSubject, disposables);

            FooterStepperViewModel.GoToNextCommand = ReactiveCommand.Create(() =>
            {
                ShowInstallationCompleteScreen = true;
                FooterStepperViewModel.Progress = Percent.One;
            }).DisposeWith(disposables);

            var canGoToPrev = this
                .WhenAnyValue(x => x.ShowInstallationCompleteScreen)
                .Select(x => x);

            FooterStepperViewModel.GoToPrevCommand = ReactiveCommand.Create(() =>
            {
                ShowInstallationCompleteScreen = false;
                FooterStepperViewModel.Progress = Percent.Zero;
            }, canGoToPrev).DisposeWith(disposables);
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
