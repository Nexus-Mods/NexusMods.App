using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerGroupViewModel : AViewModel<IGuidedInstallerGroupViewModel>, IGuidedInstallerGroupViewModel
{
    public OptionGroup Group { get; }

    public GuidedInstallerGroupViewModel(OptionGroup group)
    {
        Group = group;
    }
}
