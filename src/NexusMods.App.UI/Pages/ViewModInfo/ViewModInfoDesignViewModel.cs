using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.ModInfo.ViewModFiles;
using NexusMods.App.UI.Pages.ViewModInfo.Types;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.ViewModInfo;

public class ViewModInfoDesignViewModel : APageViewModel<IViewModInfoViewModel>, IViewModInfoViewModel
{

    public LoadoutId LoadoutId { get; set; }
    public ModId ModId { get; set; }
    public CurrentViewModInfoPage Page { get; set; }
    public IViewModelInterface PageViewModel { get; set; }
    
    // Design
    public ViewModInfoDesignViewModel() : base(null!) => PageViewModel = new ViewModFilesDesignViewModel();
}
