using System.Collections.ObjectModel;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;

namespace NexusMods.App.UI.RightContent.Downloads;

public class InProgressViewModel : AViewModel<IInProgressViewModel>, IInProgressViewModel
{
    public ReadOnlyObservableCollection<IDownloadTaskViewModel> Tasks { get; } =
        new(new ObservableCollection<IDownloadTaskViewModel>());
}
