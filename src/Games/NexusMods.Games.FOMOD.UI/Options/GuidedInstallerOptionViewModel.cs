using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerOptionViewModel : AViewModel<IGuidedInstallerOptionViewModel>, IGuidedInstallerOptionViewModel
{
    public Option Option { get; }

    [Reactive]
    public bool IsSelected { get; set; }

    public GuidedInstallerOptionViewModel(Option option)
    {
        Option = option;
    }
}
