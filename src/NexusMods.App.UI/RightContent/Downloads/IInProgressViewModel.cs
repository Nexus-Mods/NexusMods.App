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
    
    /// <summary>
    /// Total size of items currently downloaded.
    /// </summary>
    public long DownloadedSizeBytes { get; }
    
    /// <summary>
    /// Total size of items to be downloaded in bytes.
    /// </summary>
    public long TotalSizeBytes { get; }
    
    /// <summary>
    /// Seconds remaining until the download completes.
    /// </summary>
    public int SecondsRemaining { get; set; }
}
