using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using NexusMods.Paths;
using NexusMods.Paths.Trees;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class AdvancedInstallerPageDesignViewModel : AViewModel<IAdvancedInstallerPageViewModel>, IAdvancedInstallerPageViewModel
{
    public IFooterViewModel FooterViewModel { get; }
    public IBodyViewModel BodyViewModel { get; }
    public bool ShouldInstall { get; set; }

    public AdvancedInstallerPageDesignViewModel()
    {
        BodyViewModel = null!;
        FooterViewModel = null!;
        ShouldInstall = false;
    }
}
