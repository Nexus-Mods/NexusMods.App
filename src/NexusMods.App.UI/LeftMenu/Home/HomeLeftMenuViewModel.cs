using System.Collections.ObjectModel;
using DynamicData.Kernel;
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
                Name = Language.MyGames, Icon = IconType.Bookmark, Activate = ReactiveCommand.Create(() =>
                {
                    var pageData = new PageData
                    {
                        FactoryId = MyGamesPageFactory.StaticId,
                        Context = new MyGamesPageContext(),
                    };

                    // TODO: use https://github.com/Nexus-Mods/NexusMods.App/issues/942
                    var input = NavigationInput.Default;

                    var behavior = workspaceController.GetDefaultOpenPageBehavior(pageData, input, Optional<PageIdBundle>.None);
                    workspaceController.OpenPage(WorkspaceId, pageData, behavior);
                }),
            },
        };

        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(new ObservableCollection<ILeftMenuItemViewModel>(items));
    }
}
