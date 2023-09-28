using NexusMods.App.UI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class AdvancedInstallerOverlayViewModel : AViewModel<IAdvancedInstallerOverlayViewModel>, IAdvancedInstallerOverlayViewModel
{
    public bool IsActive { get; set; }
    public virtual IAdvancedInstallerFooterViewModel FooterViewModel { get; } = new AdvancedInstallerFooterViewModel();
    public virtual IAdvancedInstallerBodyViewModel BodyViewModel { get; } = new AdvancedInstallerBodyViewModel();
}
