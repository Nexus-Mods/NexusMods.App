using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;

namespace NexusMods.Games.FOMOD.UI;

public interface IGuidedInstallerGroupViewModel : IViewModel
{
    public OptionGroup Group { get; }

    public IGuidedInstallerOptionViewModel[] Options { get; }

    public IGuidedInstallerOptionViewModel? HighlightedOption { get; set;  }
}
