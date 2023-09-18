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

        this.WhenActivated(disposables =>
        {
            var canGoNext = this.WhenAnyValue(x => x.HasValidSelections);
            FooterStepperViewModel.GoToNextCommand = ReactiveCommand.Create(() =>
            {
                ShowInstallationCompleteScreen = true;
                FooterStepperViewModel.Progress = Percent.One;
            }, canGoNext).DisposeWith(disposables);

            var canGoToPrev = this.WhenAnyValue(x => x.ShowInstallationCompleteScreen);
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
