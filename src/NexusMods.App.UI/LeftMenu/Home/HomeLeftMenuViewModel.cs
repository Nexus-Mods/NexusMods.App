using System.Collections.ObjectModel;
using NexusMods.App.UI.Icons;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.RightContent;
using NexusMods.App.UI.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Home;

public class HomeLeftMenuViewModel : AViewModel<IHomeLeftMenuViewModel>, IHomeLeftMenuViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }

    [Reactive]
    public IRightContent RightContent { get; set; } =
        Initializers.IRightContent;

    public HomeLeftMenuViewModel(IFoundGamesViewModel foundGamesViewModel)
    {
        var items = new ILeftMenuItemViewModel[]
        {
            new IconViewModel { Name = "Newsfeed", Icon = IconType.News },
            new IconViewModel { Name = "My Games", Icon = IconType.Bookmark, Activate = ReactiveCommand.Create(
                () =>
                {
                    RightContent = foundGamesViewModel;
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
