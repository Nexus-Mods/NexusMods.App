using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;

namespace NexusMods.Games.FOMOD.UI;

public interface IGuidedInstallerOptionViewModel : IViewModel
{
    public Option Option { get; }

    public OptionGroup Group { get; }

    public bool IsEnabled { get; set; }

    public bool IsChecked { get; set; }

    public bool IsValid { get; set; }
}
