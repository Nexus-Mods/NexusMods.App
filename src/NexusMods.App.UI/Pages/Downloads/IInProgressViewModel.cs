using System.Collections.ObjectModel;
using System.Windows.Input;
using DynamicData;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Controls.DownloadGrid;
using NexusMods.App.UI.Pages.Downloads.ViewModels;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Downloads;

public interface IInProgressViewModel : IPageViewModelInterface
{
    /// <summary>
    /// These tasks contain only current in-progress tasks; completed tasks are removed from this list.
    /// </summary>
    ReadOnlyObservableCollection<IDownloadTaskViewModel> Tasks { get; }

    ReadOnlyObservableCollection<IDataGridColumnFactory<DownloadColumn>> Columns { get; }

    public ReadOnlyObservableCollection<ISeries> Series { get; }

    public Axis[] YAxes { get; }
    public Axis[] XAxes { get; }

    /// <summary>
    /// True if download is running, else false.
    /// </summary>
    bool HasDownloads { get; }

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
    SourceList<IDownloadTaskViewModel> SelectedTasks { get; set;}

    /// <summary>
    /// Shows the cancel 'dialog' to the user.
    /// </summary>
    ICommand ShowCancelDialogCommand { get; set; }

    /// <summary>
    /// Suspends the current task.
    /// </summary>
    ICommand SuspendSelectedTasksCommand { get; }

    /// <summary>
    /// Resumes the current task.
    /// </summary>
    ICommand ResumeSelectedTasksCommand { get; }

    /// <summary>
    /// Suspends all the tasks.
    /// </summary>
    ICommand SuspendAllTasksCommand { get; }

    /// <summary>
    /// Resumes all the tasks.
    /// </summary>
    ICommand ResumeAllTasksCommand { get; }

    /// <summary>
    /// Shows the additional settings for the current task (there is nothing for now).
    /// </summary>
    ICommand ShowSettings { get; }

    /// <summary>
    /// Cancels all the passed tasks, without asking for confirmation.
    /// </summary>
    void CancelTasks(IEnumerable<IDownloadTaskViewModel> tasks);

    /// <summary>
    /// Suspends all the "Downloading" passed tasks.
    /// </summary>
    void SuspendTasks(IEnumerable<IDownloadTaskViewModel> tasks);

    /// <summary>
    /// Resumes all the "Paused" passed tasks.
    /// </summary>
    void ResumeTasks(IEnumerable<IDownloadTaskViewModel> tasks);

}
