using System.Collections.ObjectModel;
using System.Windows.Input;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

namespace NexusMods.App.UI.RightContent.Downloads;

public interface IInProgressViewModel : IRightContentViewModel
{
    /// <summary>
    /// These tasks contain only current in-progress tasks; completed tasks are removed from this list.
    /// </summary>
    ReadOnlyObservableCollection<IDownloadTaskViewModel> Tasks { get; }

    ReadOnlyObservableCollection<IDataGridColumnFactory> Columns { get; }

    /// <summary>
    /// This command cancels the currently selected task.
    /// </summary>
    void CancelSelectedTask();

    /// <summary>
    /// True if download is running, else false.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Total size of items currently downloaded.
    /// </summary>
    long DownloadedSizeBytes { get; }

    /// <summary>
    /// Total size of items to be downloaded in bytes.
    /// </summary>
    long TotalSizeBytes { get; }

    /// <summary>
    /// Seconds remaining until the download completes.
    /// </summary>
    int SecondsRemaining { get; set; }

    /// <summary>
    /// The currently selected task.
    /// </summary>
    IDownloadTaskViewModel? SelectedTask { get; set; }

    /// <summary>
    /// Cancels the selected task.
    /// </summary>
    void Cancel() => SelectedTask?.Cancel();

    /// <summary>
    /// Shows the cancel 'dialog' to the user.
    /// </summary>
    ICommand ShowCancelDialog { get; }
}
