using NexusMods.Abstractions.UI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class AdvancedInstallerWindowDesignViewModel : AViewModel<IAdvancedInstallerWindowViewModel>,
    IAdvancedInstallerWindowViewModel
{
    public IViewModelInterface CurrentPageVM { get; }
    public IUnsupportedModPageViewModel UnsupportedModVM { get; }
    public IAdvancedInstallerPageViewModel AdvancedInstallerVM { get; }
    
    public AdvancedInstallerWindowDesignViewModel()
    {
        UnsupportedModVM = new UnsupportedModPageDesignViewModel();
        AdvancedInstallerVM = new AdvancedInstallerPageDesignViewModel();
        CurrentPageVM = AdvancedInstallerVM; // Default to Unsupported Mod page in design mode
    }
}
