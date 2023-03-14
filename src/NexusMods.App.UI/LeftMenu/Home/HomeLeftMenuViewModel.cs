using System.Collections.ObjectModel;
using NexusMods.App.UI.Icons;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.RightContent;
using NexusMods.App.UI.RightContent.Home;
using NexusMods.App.UI.RightContent.MyGames;
using NexusMods.App.UI.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Home;

public class HomeLeftMenuViewModel : AViewModel<IHomeLeftMenuViewModel>, IHomeLeftMenuViewModel
{
    private readonly IHomeViewModel _homeViewModel;
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }

    [Reactive]
    public IRightContentViewModel RightContent { get; set; } =
        Initializers.IRightContent;

    public HomeLeftMenuViewModel(IMyGamesViewModel myGamesViewModel, IFoundGamesViewModel foundGamesViewModel, IHomeViewModel homeViewModel)
    {
        _homeViewModel = homeViewModel;
        var items = new ILeftMenuItemViewModel[]
        {
            new IconViewModel { Name = "Newsfeed", Icon = IconType.News, Activate = ReactiveCommand.Create(
                () =>
                {
                    RightContent = _homeViewModel;
                })},
            new IconViewModel { Name = "My Games", Icon = IconType.Bookmark, Activate = ReactiveCommand.Create(
                () =>
                {
                    RightContent = myGamesViewModel;
                }) },
            new IconViewModel { Name = "Browse Games", Icon = IconType.Game, Activate = ReactiveCommand.Create(
                () =>
                {
                    RightContent = foundGamesViewModel;
                })}
        };
        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(
            new ObservableCollection<ILeftMenuItemViewModel>(items));
    }
}
