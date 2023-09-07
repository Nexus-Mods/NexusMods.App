using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerGroupViewModel : AViewModel<IGuidedInstallerGroupViewModel>, IGuidedInstallerGroupViewModel
{
    public OptionGroup Group { get; }

    public IGuidedInstallerOptionViewModel[] Options { get; }

    [Reactive]
    public IGuidedInstallerOptionViewModel? HighlightedOption { get; set; }

    public GuidedInstallerGroupViewModel(OptionGroup group) : this(group, option => new GuidedInstallerOptionViewModel(option)) { }

    protected GuidedInstallerGroupViewModel(OptionGroup group, Func<Option, IGuidedInstallerOptionViewModel> factory)
    {
        Group = group;
        Options = group.Options
            .Select(factory)
            .ToArray();
    }
}
