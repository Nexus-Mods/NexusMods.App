using NexusMods.App.UI;
using NexusMods.Games.AdvancedInstaller.UI.Content;
using NexusMods.Games.AdvancedInstaller.UI.Content.Bottom;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class AdvancedInstallerOverlayViewModel : AViewModel<IAdvancedInstallerOverlayViewModel>,
    IAdvancedInstallerOverlayViewModel
{
    public bool IsActive { get; set; }
    public virtual IFooterViewModel FooterViewModel { get; } = new FooterViewModel();
    public virtual IBodyViewModel BodyViewModel { get; } = new BodyViewModel();
}
