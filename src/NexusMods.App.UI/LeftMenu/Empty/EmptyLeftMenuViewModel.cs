using NexusMods.Abstractions.UI;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;


namespace NexusMods.App.UI.LeftMenu;

public class EmptyLeftMenuViewModel : AViewModel<IEmptyLeftMenuViewModel>, IEmptyLeftMenuViewModel
{
    public WorkspaceId WorkspaceId { get; }
    
    public ILeftMenuItemViewModel LeftMenuItemEmpty { get; }
    
    public EmptyLeftMenuViewModel(WorkspaceId workspaceId, string message)
    {
        WorkspaceId = workspaceId;
        
        LeftMenuItemEmpty = new LeftMenuItemDesignViewModel
        {
            Text = message,
            Icon = IconValues.Alert,
        };
    }
}
