using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.ModInfo.ViewModFiles;
using NexusMods.App.UI.Pages.ModInfo.Types;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.ModInfo;

public class ModInfoViewModInfoDesignModInfoViewModel : APageViewModel<IModInfoViewModel>, IModInfoViewModel
{
    public LoadoutId LoadoutId { get; set; }
    public ModId ModId { get; set; }
    public CurrentModInfoPage Page { get; set; }
    public IViewModelInterface PageViewModel { get; set; }
    
    // Design
    public ModInfoViewModInfoDesignModInfoViewModel() : base(new DesignWindowManager()) => PageViewModel = new ViewModFilesDesignViewModel();
}
