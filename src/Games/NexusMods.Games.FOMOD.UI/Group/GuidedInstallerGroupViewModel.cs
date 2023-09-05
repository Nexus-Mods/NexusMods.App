using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerGroupViewModel : AViewModel<IGuidedInstallerGroupViewModel>, IGuidedInstallerGroupViewModel
{
    public OptionGroup Group { get; }

    public IGuidedInstallerOptionViewModel[] Options { get; set; }

    public GuidedInstallerGroupViewModel(OptionGroup group)
    {
        Group = group;

        Options = group.Options
            .Select(option => (IGuidedInstallerOptionViewModel) new GuidedInstallerOptionViewModel(option))
            .ToArray();
    }
}
