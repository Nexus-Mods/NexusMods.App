using NexusMods.App.UI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class AdvancedInstallerBodyViewModel : AViewModel<IAdvancedInstallerBodyViewModel>, IAdvancedInstallerBodyViewModel
{
    public IAdvancedInstallerModContentViewModel ModContentViewModel { get; } = new AdvancedInstallerModContentViewModel();
}
