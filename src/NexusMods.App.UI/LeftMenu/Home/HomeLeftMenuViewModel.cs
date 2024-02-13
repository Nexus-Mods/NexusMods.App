using System.Collections.ObjectModel;
using JetBrains.Annotations;
using NexusMods.App.UI.Icons;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Pages.MyGames;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Home;

[UsedImplicitly]
public class HomeLeftMenuViewModel : AViewModel<IHomeLeftMenuViewModel>, IHomeLeftMenuViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }

    public HomeLeftMenuViewModel(IMyGamesViewModel myGamesViewModel, WorkspaceId workspaceId, IWorkspaceController workspaceController)
    {
        var items = new ILeftMenuItemViewModel[]
        {
            new IconViewModel
            {
                Name = Language.MyGames, Icon = IconType.Bookmark, Activate = ReactiveCommand.Create(
                    () =>
                    {
                        workspaceController.OpenPage(workspaceId,
                            new PageData
                            {
                                FactoryId = MyGamesPageFactory.StaticId,
                                Context = new MyGamesPageContext()
                            },
                            new OpenPageBehavior(new OpenPageBehavior.PrimaryDefault()));
                    })
            }
        };
        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(new ObservableCollection<ILeftMenuItemViewModel>(items));
    }
}
