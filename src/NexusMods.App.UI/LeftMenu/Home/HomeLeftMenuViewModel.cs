using System.Collections.ObjectModel;
using JetBrains.Annotations;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Pages.MyGames;
using NexusMods.App.UI.Pages.MyLoadouts;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Home;

[UsedImplicitly]
public class HomeLeftMenuViewModel : AViewModel<IHomeLeftMenuViewModel>, IHomeLeftMenuViewModel
{
    public WorkspaceId WorkspaceId { get; }
    public ILeftMenuItemViewModel LeftMenuItemMyGames { get; }
    public ILeftMenuItemViewModel LeftMenuItemMyLoadouts { get; }

    public HomeLeftMenuViewModel(
        IMyGamesViewModel myGamesViewModel,
        WorkspaceId workspaceId,
        IWorkspaceController workspaceController)
    {
        WorkspaceId = workspaceId;

        LeftMenuItemMyGames = new LeftMenuItemViewModel(
            workspaceController,
            WorkspaceId,
            new PageData
            {
                FactoryId = MyGamesPageFactory.StaticId,
                Context = new MyGamesPageContext(),
            }
        )
        {
            Text = new StringComponent(Language.MyGames),
            Icon = IconValues.GamepadOutline,
        };
        
        LeftMenuItemMyLoadouts = new LeftMenuItemViewModel(
            workspaceController,
            WorkspaceId,
            new PageData
            {
                FactoryId = MyLoadoutsPageFactory.StaticId,
                Context = new MyLoadoutsPageContext(),
            }
        )
        {
            Text = new StringComponent(Language.MyLoadoutsPageTitle),
            Icon = IconValues.Package,
        };
    }
}
