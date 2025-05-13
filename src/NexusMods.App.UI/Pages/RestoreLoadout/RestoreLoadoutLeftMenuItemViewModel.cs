using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.RestoreLoadout;

public class RestoreLoadoutLeftMenuItemViewModel : LeftMenuItemViewModel
{
    public RestoreLoadoutLeftMenuItemViewModel(
        IWorkspaceController workspaceController,
        ISettingsManager settingsManager,
        WorkspaceId workspaceId,
        PageData pageData,
        LoadoutId loadoutId) : base(workspaceController, workspaceId, pageData)
    {
        
    }
}
