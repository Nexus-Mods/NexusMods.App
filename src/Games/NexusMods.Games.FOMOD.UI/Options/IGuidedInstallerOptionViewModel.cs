using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;

namespace NexusMods.Games.FOMOD.UI;

public interface IGuidedInstallerOptionViewModel : IViewModel
{
    public Option Option { get; }

    public bool IsEnabled { get; set; }

    public bool IsSelected { get; set; }
}
