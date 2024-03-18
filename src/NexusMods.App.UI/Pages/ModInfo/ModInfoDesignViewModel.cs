using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.ModInfo.ModFiles;
using NexusMods.App.UI.Pages.ModInfo.Types;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.ModInfo;

public class ModInfoDesignViewModel : APageViewModel<IModInfoViewModel>, IModInfoViewModel
{
    public LoadoutId LoadoutId { get; set; }
    public ModId ModId { get; set; }
    public CurrentModInfoSection Section { get; set; }
    public IViewModelInterface SectionViewModel { get; set; }
    
    // Design
    public ModInfoDesignViewModel() : base(new DesignWindowManager()) => SectionViewModel = new ModFilesDesignViewModel();
}
