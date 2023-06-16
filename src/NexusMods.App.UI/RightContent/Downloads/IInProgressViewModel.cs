using System.Collections.ObjectModel;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

namespace NexusMods.App.UI.RightContent.Downloads;

public interface IInProgressViewModel : IRightContentViewModel
{
    ReadOnlyObservableCollection<IDownloadTaskViewModel> Tasks { get; }
    public ReadOnlyObservableCollection<IDataGridColumnFactory> Columns { get; }
}
