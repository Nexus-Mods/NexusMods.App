using System.Reactive.Disposables;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using NexusMods.Common.GuidedInstaller.ValueObjects;
using NexusMods.DataModel.RateLimiting;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerStepDesignViewModel : AGuidedInstallerStepViewModel
{
    public override string? ModName { get; set; } = "Example Mod";
    public override IFooterStepperViewModel FooterStepperViewModel { get; } = new FooterStepperDesignViewModel(Percent.Zero);

    public GuidedInstallerStepDesignViewModel() : this(null!) { }

    public GuidedInstallerStepDesignViewModel(IImageCache imageCache) : base(imageCache)
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

        var goToNextCommand = ReactiveCommand.Create(() =>
        {
            ShowInstallationCompleteScreen = true;
            FooterStepperViewModel.Progress = Percent.One;
        });

        var goToPrevCommand = ReactiveCommand.Create(() =>
        {
            ShowInstallationCompleteScreen = false;
            FooterStepperViewModel.Progress = Percent.Zero;
        });

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.HasValidSelections)
                .BindTo(this, vm => vm.FooterStepperViewModel.CanGoNext)
                .DisposeWith(disposables);

            this.WhenAnyValue(vm => vm.ShowInstallationCompleteScreen)
                .BindTo(this, vm => vm.FooterStepperViewModel.CanGoPrev)
                .DisposeWith(disposables);

            this.WhenAnyObservable(vm => vm.FooterStepperViewModel.GoToNextCommand)
                .InvokeCommand(goToNextCommand)
                .DisposeWith(disposables);

            this.WhenAnyObservable(vm => vm.FooterStepperViewModel.GoToPrevCommand)
                .InvokeCommand(goToPrevCommand)
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
