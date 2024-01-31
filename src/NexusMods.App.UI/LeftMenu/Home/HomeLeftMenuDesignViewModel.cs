using System.Collections.ObjectModel;
using NexusMods.App.UI.Icons;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.RightContent;

namespace NexusMods.App.UI.LeftMenu.Home;

public class HomeLeftMenuDesignViewModel : AViewModel<IHomeLeftMenuViewModel>, IHomeLeftMenuViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }
    public IRightContentViewModel RightContent => Initializers.IRightContent;

    public HomeLeftMenuDesignViewModel()
    {
        var items = new ILeftMenuItemViewModel[]
        {
            new IconViewModel { Name = Language.MyGames, Icon = IconType.Bookmark },
        };
        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(new ObservableCollection<ILeftMenuItemViewModel>(items));
    }
}
