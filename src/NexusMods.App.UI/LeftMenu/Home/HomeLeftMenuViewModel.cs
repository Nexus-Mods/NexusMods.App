using System.Collections.ObjectModel;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Pages.MyGames;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Home;

[UsedImplicitly]
public class HomeLeftMenuViewModel : AViewModel<IHomeLeftMenuViewModel>, IHomeLeftMenuViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }
    public WorkspaceId WorkspaceId { get; }

    public HomeLeftMenuViewModel(
        IMyGamesViewModel myGamesViewModel,
        WorkspaceId workspaceId,
        IWorkspaceController workspaceController)
    {
        WorkspaceId = workspaceId;
        var items = new ILeftMenuItemViewModel[]
        {
            new IconViewModel
            {
                Name = Language.MyGames,
                Icon = IconValues.JoystickGameFilled,
                NavigateCommand = ReactiveCommand.Create<NavigationInformation>(info =>
                {
                    var pageData = new PageData
                    {
                        FactoryId = MyGamesPageFactory.StaticId,
                        Context = new MyGamesPageContext(),
                    };

                    var behavior = workspaceController.GetOpenPageBehavior(pageData, info, Optional<PageIdBundle>.None);
                    workspaceController.OpenPage(WorkspaceId, pageData, behavior);
                }),
            },
        };

        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(new ObservableCollection<ILeftMenuItemViewModel>(items));
    }
}
