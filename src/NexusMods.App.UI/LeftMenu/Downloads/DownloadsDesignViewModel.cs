using System.Collections.ObjectModel;
using NexusMods.App.UI.RightContent;

namespace NexusMods.App.UI.LeftMenu.Downloads;

public class DownloadsDesignViewModel : AViewModel<IDownloadsViewModel>, IDownloadsViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; } =
        Initializers.ReadOnlyObservableCollection<ILeftMenuItemViewModel>();
    public IRightContentViewModel RightContent { get; } = Initializers.IRightContent;

}
