using System.Collections.ObjectModel;
using System.Windows.Input;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.RightContent.DownloadGrid;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;

namespace NexusMods.App.UI.RightContent.Downloads;

public interface IInProgressViewModel : IRightContentViewModel
{
    /// <summary>
    /// These tasks contain only current in-progress tasks; completed tasks are removed from this list.
    /// </summary>
    ReadOnlyObservableCollection<IDownloadTaskViewModel> Tasks { get; }

    ReadOnlyObservableCollection<IDataGridColumnFactory<DownloadColumn>> Columns { get; }

    /// <summary>
    /// This command cancels the currently selected task.
    /// </summary>
    void CancelSelectedTask();

    /// <summary>
    /// This command suspends the currently selected task.
    /// </summary>
    void SuspendSelectedTask();

    /// <summary>
    /// True if download is running, else false.
    /// </summary>
    bool IsRunning { get; }

    int ActiveDownloadCount { get; set; }

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
    /// Suspends the selected task.
    /// </summary>
    void Suspend() => SelectedTask?.Suspend();

    /// <summary>
    /// Shows the cancel 'dialog' to the user.
    /// </summary>
    ICommand ShowCancelDialog { get; }

    /// <summary>
    /// Suspends the current task.
    /// </summary>
    ICommand SuspendCurrentTask { get; }

    /// <summary>
    /// Suspends all the tasks.
    /// </summary>
    ICommand SuspendAllTasks { get; }
}
