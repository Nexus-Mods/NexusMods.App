using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Pages.ModInfo.Types;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.ModInfo;

public interface IModInfoViewModel : IPageViewModelInterface
{ 
    LoadoutId LoadoutId { get; set; }
    ModId ModId { get; set; }
    CurrentModInfoSection Section { get; set; }
    IViewModelInterface SectionViewModel { get; set; }
    void SetContext(ModInfoPageContext context) => SetContextImpl(this, context);

    internal static void SetContextImpl(IModInfoViewModel vm, ModInfoPageContext context) 
    {
        vm.LoadoutId = context.LoadoutId;
        vm.ModId = context.ModId;
        vm.Section = context.Section;
    }
}

