using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using NexusMods.Common.GuidedInstaller.ValueObjects;
using NexusMods.Games.FOMOD.UI.Resources;
using NexusMods.Games.FOMOD.UI.Resources;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerGroupViewModel : AViewModel<IGuidedInstallerGroupViewModel>, IGuidedInstallerGroupViewModel
{
    [Reactive]
    public bool HasValidSelection { get; set; } = true;

    public OptionGroup Group { get; }

    public IGuidedInstallerOptionViewModel[] Options { get; }

    [Reactive]
    public IGuidedInstallerOptionViewModel? HighlightedOption { get; set; }

    public GuidedInstallerGroupViewModel(OptionGroup group) : this(group, option => new GuidedInstallerOptionViewModel(option, group)) { }

    protected GuidedInstallerGroupViewModel(OptionGroup group, Func<Option, IGuidedInstallerOptionViewModel> factory)
    {
        Group = group;

        var options = group.Options.Select(factory);
        if (group.Type == OptionGroupType.AtMostOne)
        {
            Options = options
                .Prepend(factory(new Option
                {
                    Id = GuidedInstallerStepViewModelHelpers.NoneOptionId,
                    Name = Language.GuidedInstallerGroupViewModel_GuidedInstallerGroupViewModel_None,
                    Type = OptionType.Available,
                    Description = Language.GuidedInstallerGroupViewModel_GuidedInstallerGroupViewModel_Select_nothing,
                    HoverText = Language.GuidedInstallerGroupViewModel_GuidedInstallerGroupViewModel_Use_this_option_to_select_nothing
                }))
                .ToArray();
        }
        else
        {
            Options = options.ToArray();
        }

        this.WhenAnyValue(x => x.HasValidSelection)
            .SubscribeWithErrorLogging(logger: default, isValid =>
            {
                foreach (var optionVM in Options)
                {
                    optionVM.IsValid = isValid;
                }
            });
    }
}
