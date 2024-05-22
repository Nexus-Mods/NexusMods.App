using System.Collections.ObjectModel;
using System.Reactive;
using DynamicData;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Controls.DownloadGrid;
using NexusMods.App.UI.Pages.Downloads.ViewModels;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Downloads;

public interface IInProgressViewModel : IPageViewModelInterface
{
    /// <summary>
    /// Collection of in progress download tasks (downloading, paused, etc.)
    /// </summary>
    ReadOnlyObservableCollection<IDownloadTaskViewModel> InProgressTasks { get; }
    
    /// <summary>
    /// Collection of completed download tasks
    /// </summary>
    ReadOnlyObservableCollection<IDownloadTaskViewModel> CompletedTasks { get; }

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
    SourceList<IDownloadTaskViewModel> SelectedInProgressTasks { get; }
    
    
    SourceList<IDownloadTaskViewModel> SelectedCompletedTasks { get; }

    /// <summary>
    /// Shows the cancel 'dialog' to the user.
    /// </summary>
    ReactiveCommand<Unit,Unit> ShowCancelDialogCommand { get; }

    /// <summary>
    /// Suspends the current task.
    /// </summary>
    ReactiveCommand<Unit,Unit> SuspendSelectedTasksCommand { get; }

    /// <summary>
    /// Resumes the current task.
    /// </summary>
    ReactiveCommand<Unit,Unit> ResumeSelectedTasksCommand { get; }

    /// <summary>
    /// Suspends all the tasks.
    /// </summary>
    ReactiveCommand<Unit,Unit> SuspendAllTasksCommand { get; }

    /// <summary>
    /// Resumes all the tasks.
    /// </summary>
    ReactiveCommand<Unit,Unit> ResumeAllTasksCommand { get; }
    
    ReactiveCommand<Unit, Unit> HideSelectedCommand { get; }
    
    ReactiveCommand<Unit, Unit> HideAllCommand { get; }

}
