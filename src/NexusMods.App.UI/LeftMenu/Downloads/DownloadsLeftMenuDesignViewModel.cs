using System.Collections.ObjectModel;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;

namespace NexusMods.App.UI.LeftMenu.Downloads;

public class DownloadsLeftMenuDesignViewModel : AViewModel<IDownloadsLeftMenuViewModel>, IDownloadsLeftMenuViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }
    public WorkspaceId WorkspaceId { get; } = WorkspaceId.NewId();

    public DownloadsLeftMenuDesignViewModel()
    {
        var items = new ILeftMenuItemViewModel[]
        {
            new IconViewModel
            {
                Name = Language.InProgressTitleTextBlock,
                Icon = IconValues.Downloading,
            },
        };
        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(new ObservableCollection<ILeftMenuItemViewModel>(items));
    }
}
