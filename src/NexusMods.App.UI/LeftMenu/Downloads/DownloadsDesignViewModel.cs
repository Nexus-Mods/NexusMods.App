using System.Collections.ObjectModel;
using NexusMods.App.UI.RightContent;
using NexusMods.App.UI.RightContent.Downloads;
using NexusMods.App.UI.ViewModels.Helpers.ViewModelSelector;

namespace NexusMods.App.UI.LeftMenu.Downloads;

public class DownloadsDesignViewModel : ViewModelDesignSelector<Options, IRightContentViewModel>, IDownloadsViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; } =
        Initializers.ReadOnlyObservableCollection<ILeftMenuItemViewModel>();
    public IRightContentViewModel RightContent { get; } = Initializers.IRightContent;

    public DownloadsDesignViewModel() :
        base(new InProgressDesignViewModel(),
            new CompletedDesignViewModel(),
            new HistoryDesignViewModel())
    {

    }
}
