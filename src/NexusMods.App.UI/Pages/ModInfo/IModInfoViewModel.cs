using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Pages.ModInfo.Types;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.ModInfo;

public interface IModInfoViewModel : IPageViewModelInterface
{ 
    LoadoutId LoadoutId { get; set; }
    ModId ModId { get; set; }
    CurrentModInfoPage Page { get; set; }
    IViewModelInterface PageViewModel { get; set; }

    /// <summary>
    ///     Copies information about current page from a given 'context'.
    /// </summary>
    void SetContext(ViewModInfoPageContext context)
    {
        LoadoutId = context.LoadoutId;
        ModId = context.ModId;
        Page = context.Page;
    }
}
