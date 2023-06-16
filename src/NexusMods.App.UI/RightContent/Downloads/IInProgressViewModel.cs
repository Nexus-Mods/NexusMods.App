using System.Collections.ObjectModel;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

namespace NexusMods.App.UI.RightContent.Downloads;

public interface IInProgressViewModel : IRightContentViewModel
{
    /// <summary>
    /// These tasks contain only current in-progress tasks; completed tasks are removed from this list.
    /// </summary>
    ReadOnlyObservableCollection<IDownloadTaskViewModel> Tasks { get; }
    
    public ReadOnlyObservableCollection<IDataGridColumnFactory> Columns { get; }
    
    /// <summary>
    /// True if download is running, else false.
    /// </summary>
    public bool IsRunning { get; }
}
