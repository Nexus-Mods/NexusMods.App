using System.Collections.ObjectModel;
using NexusMods.App.UI.Icons;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.RightContent;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Downloads;

public class DownloadsDesignViewModel : AViewModel<IDownloadsViewModel>, IDownloadsViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }

    [Reactive]
    public IRightContentViewModel RightContent { get; set; } = Initializers.IRightContent;

    public DownloadsDesignViewModel()
    {
        var items = new ILeftMenuItemViewModel[]
        {
            new IconViewModel { Name = Language.InProgressTitleTextBlock, Icon = IconType.None },
        };
        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(new ObservableCollection<ILeftMenuItemViewModel>(items));
    }
}
