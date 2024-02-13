using System.Collections.ObjectModel;
using NexusMods.App.UI.Icons;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Resources;

namespace NexusMods.App.UI.LeftMenu.Downloads;

public class DownloadsLeftMenuDesignViewModel : AViewModel<IDownloadsLeftMenuViewModel>, IDownloadsLeftMenuViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }

    public DownloadsLeftMenuDesignViewModel()
    {
        var items = new ILeftMenuItemViewModel[]
        {
            new IconViewModel { Name = Language.InProgressTitleTextBlock, Icon = IconType.None },
        };
        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(new ObservableCollection<ILeftMenuItemViewModel>(items));
    }
}
