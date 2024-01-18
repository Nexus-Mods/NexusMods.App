using System.Collections.ObjectModel;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
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
    /// True if download is running, else false.
    /// </summary>
    bool IsRunning { get; }

    int ActiveDownloadCount { get; }

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
    int SecondsRemaining { get; }

    /// <summary>
    /// The currently selected task.
    /// </summary>
    IDownloadTaskViewModel? SelectedTask { get; set; }

    /// <summary>
    /// The currently selected task.
    /// </summary>
    SourceList<IDownloadTaskViewModel> SelectedTasks { get; set;}

    /// <summary>
    /// Shows the cancel 'dialog' to the user.
    /// </summary>
    ICommand ShowCancelDialog { get; }

    /// <summary>
    /// Suspends the current task.
    /// </summary>
    ICommand SuspendCurrentTask { get; }

    /// <summary>
    /// Resumes the current task.
    /// </summary>
    ICommand ResumeCurrentTask { get; }

    /// <summary>
    /// Suspends all the tasks.
    /// </summary>
    ICommand SuspendAllTasks { get; }

    /// <summary>
    /// Resumes all the tasks.
    /// </summary>
    ICommand ResumeAllTasks { get; }
}
