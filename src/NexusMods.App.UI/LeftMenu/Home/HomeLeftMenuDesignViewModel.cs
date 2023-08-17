using System.Collections.ObjectModel;
using NexusMods.App.UI.Icons;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.RightContent;

namespace NexusMods.App.UI.LeftMenu.Home;

public class HomeLeftMenuDesignViewModel : AViewModel<IHomeLeftMenuViewModel>, IHomeLeftMenuViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }
    public IRightContentViewModel RightContent { get; } = Initializers.IRightContent;

    public HomeLeftMenuDesignViewModel()
    {
        var items = new ILeftMenuItemViewModel[]
        {
            new IconViewModel(() => Language.Newsfeed) { Icon = IconType.News},
            new IconViewModel(() => Language.MyGames) { Icon = IconType.Bookmark },
            new IconViewModel(() => Language.BrowseGames) { Icon = IconType.Game }
        };
        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(new ObservableCollection<ILeftMenuItemViewModel>(items));
    }
}
