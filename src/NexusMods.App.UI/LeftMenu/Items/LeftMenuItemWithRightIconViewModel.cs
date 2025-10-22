using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.LeftMenu.Items;

public class LeftMenuItemWithRightIconViewModel : LeftMenuItemViewModel
{
    public required IconValue RightIcon { get; init; }

    public LeftMenuItemWithRightIconViewModel(
        IWorkspaceController workspaceController,
        WorkspaceId workspaceId,
        PageData pageData) : base(workspaceController, workspaceId, pageData) { }
}
