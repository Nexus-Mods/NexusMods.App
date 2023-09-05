using System.Collections.ObjectModel;
using NexusMods.App.UI.Icons;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.RightContent;
using NexusMods.App.UI.RightContent.MyGames;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Home;

public class HomeLeftMenuViewModel : AViewModel<IHomeLeftMenuViewModel>, IHomeLeftMenuViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }

    [Reactive] public IRightContentViewModel RightContent { get; set; } = Initializers.IRightContent;

    public HomeLeftMenuViewModel(IMyGamesViewModel myGamesViewModel, IFoundGamesViewModel foundGamesViewModel)
    {
        var items = new ILeftMenuItemViewModel[]
        {
            new IconViewModel
            {
                Name = Language.Newsfeed, Icon = IconType.News, Activate = ReactiveCommand.Create(
                    () => { RightContent = Initializers.IRightContent; })
            },
            new IconViewModel
            {
                Name = Language.MyGames, Icon = IconType.Bookmark, Activate = ReactiveCommand.Create(
                    () => { RightContent = myGamesViewModel; })
            },
            new IconViewModel
            {
                Name = Language.BrowseGames, Icon = IconType.Game, Activate = ReactiveCommand.Create(
                    () => { RightContent = foundGamesViewModel; })
            }
        };
        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(
            new ObservableCollection<ILeftMenuItemViewModel>(items));
    }
}
