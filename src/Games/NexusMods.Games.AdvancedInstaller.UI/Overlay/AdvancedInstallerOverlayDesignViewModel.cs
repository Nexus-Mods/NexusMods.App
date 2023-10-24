using NexusMods.Games.AdvancedInstaller.UI.Content;
using NexusMods.Games.AdvancedInstaller.UI.Content.Bottom;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class AdvancedInstallerOverlayDesignViewModel : AViewModel<IAdvancedInstallerOverlayViewModel>,
    IAdvancedInstallerOverlayViewModel
{
    public bool IsActive { get; set; }
    public IFooterViewModel FooterViewModel { get; } = new FooterDesignViewModel();
    public IBodyViewModel BodyViewModel { get; } = new BodyDesignViewModel();
}
