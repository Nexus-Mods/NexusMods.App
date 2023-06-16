using System.Collections.ObjectModel;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

namespace NexusMods.App.UI.RightContent.Downloads;

public class InProgressViewModel : AViewModel<IInProgressViewModel>, IInProgressViewModel
{
    public ReadOnlyObservableCollection<IDownloadTaskViewModel> Tasks { get; } =
        new(new ObservableCollection<IDownloadTaskViewModel>());
    
    private ReadOnlyObservableCollection<IDataGridColumnFactory>
        _filteredColumns = new(new ObservableCollection<IDataGridColumnFactory>());
    public ReadOnlyObservableCollection<IDataGridColumnFactory> Columns => _filteredColumns;

}
