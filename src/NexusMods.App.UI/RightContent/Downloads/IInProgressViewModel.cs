using System.Collections.ObjectModel;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;

namespace NexusMods.App.UI.RightContent.Downloads;

public interface IInProgressViewModel : IRightContentViewModel
{
    ReadOnlyObservableCollection<IDownloadTaskViewModel> Tasks { get; }
}
