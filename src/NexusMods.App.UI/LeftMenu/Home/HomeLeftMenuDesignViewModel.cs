using System.Collections.ObjectModel;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;

namespace NexusMods.App.UI.LeftMenu.Home;

public class HomeLeftMenuDesignViewModel : AViewModel<IHomeLeftMenuViewModel>, IHomeLeftMenuViewModel
{
    public WorkspaceId WorkspaceId { get; } = WorkspaceId.NewId();
    
    public ILeftMenuItemViewModel LeftMenuItemMyGames { get; } = new LeftMenuItemDesignViewModel
    {
        Text = Language.MyGames,
        Icon = IconValues.GamepadOutline,
    };
    public ILeftMenuItemViewModel LeftMenuItemMyLoadouts { get; } = new LeftMenuItemDesignViewModel
    {
        Text = Language.MyLoadoutsPageTitle,
        Icon = IconValues.Package,
    };
    
}
