using System.Diagnostics.CodeAnalysis;
using NexusMods.Games.AdvancedInstaller.UI.Content;
using NexusMods.Games.AdvancedInstaller.UI.Content.Bottom;

namespace NexusMods.Games.AdvancedInstaller.UI;

[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
public class AdvancedInstallerOverlayDesignViewModel : AViewModel<IAdvancedInstallerOverlayViewModel>,
    IAdvancedInstallerOverlayViewModel
{
    public bool IsActive { get; set; }
    public IFooterViewModel FooterViewModel { get; } = new FooterDesignViewModel();
    public IBodyViewModel BodyViewModel { get; } = new BodyDesignViewModel();

    public bool WasCancelled { get; } = false;

    public string ModName { get; set; } = "Design Mod Name";
}
