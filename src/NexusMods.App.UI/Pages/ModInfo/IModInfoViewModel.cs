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
    void SetContext(ViewModInfoPageContext context) => SetContextImpl(this, context);

    internal static void SetContextImpl(IModInfoViewModel vm, ViewModInfoPageContext context) 
    {
        vm.LoadoutId = context.LoadoutId;
        vm.ModId = context.ModId;
        vm.Page = context.Page;
    }
}

