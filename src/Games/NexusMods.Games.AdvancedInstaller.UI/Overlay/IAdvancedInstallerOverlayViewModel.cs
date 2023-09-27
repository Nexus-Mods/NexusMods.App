using NexusMods.App.UI.Overlays;

namespace NexusMods.Games.AdvancedInstaller.UI;

public interface IAdvancedInstallerOverlayViewModel : IOverlayViewModel
{
    public IAdvancedInstallerFooterViewModel? FooterViewModel { get; }

    public IAdvancedInstallerBodyViewModel? BodyViewModel { get; }
}
