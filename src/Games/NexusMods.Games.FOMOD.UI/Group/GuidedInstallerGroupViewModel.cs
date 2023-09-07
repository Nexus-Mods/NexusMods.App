using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using NexusMods.Common.GuidedInstaller.ValueObjects;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerGroupViewModel : AViewModel<IGuidedInstallerGroupViewModel>, IGuidedInstallerGroupViewModel
{
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
                    Name = "None",
                    Type = OptionType.Available,
                    Description = "Select nothing",
                    HoverText = "Use this option to select nothing"
                }))
                .ToArray();
        }
        else
        {
            Options = options.ToArray();
        }
    }
}
