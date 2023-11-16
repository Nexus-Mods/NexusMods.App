using System.Diagnostics.CodeAnalysis;

namespace NexusMods.Games.AdvancedInstaller.UI;

[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
public class AdvancedInstallerPageDesignViewModel : AViewModel<IAdvancedInstallerPageViewModel>,
    IAdvancedInstallerPageViewModel
{
    public IFooterViewModel FooterViewModel { get; } = new FooterDesignViewModel();
    public IBodyViewModel BodyViewModel { get; } = new BodyDesignViewModel();

    public bool ShouldInstall { get; } = false;

    public string ModName { get; set; } = "Design Mod Name";
}
