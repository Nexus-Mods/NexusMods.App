using NexusMods.App.UI.Overlays;
using NexusMods.Games.AdvancedInstaller.UI.Content;
using NexusMods.Games.AdvancedInstaller.UI.Content.Bottom;

namespace NexusMods.Games.AdvancedInstaller.UI;

public interface IAdvancedInstallerOverlayViewModel : IOverlayViewModel
{
    public IFooterViewModel FooterViewModel { get; }

    public IBodyViewModel BodyViewModel { get; }

    public bool WasCancelled { get; }
}
