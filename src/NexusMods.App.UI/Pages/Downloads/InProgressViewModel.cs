using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Controls.DownloadGrid;
using NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadGameName;
using NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadName;
using NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadSize;
using NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadStatus;
using NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadVersion;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Pages.Downloads.ViewModels;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Networking.Downloaders.Interfaces;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Downloads;

public class InProgressViewModel : APageViewModel<IInProgressViewModel>, IInProgressViewModel
{
    internal const int PollTimeMilliseconds = 1000;

    private IObservable<Unit> Tick { get; set; } = Observable.Defer(() =>
        Observable.Interval(TimeSpan.FromMilliseconds(PollTimeMilliseconds))
            .Select(_ => Unit.Default));

    /// <summary>
    /// For designTime and Testing, provides an alternative list of tasks to use.
    /// </summary>
    protected readonly SourceList<IDownloadTaskViewModel> DesignTimeDownloadTasks = new();

    private ReadOnlyObservableCollection<IDownloadTaskViewModel> _tasksObservable =
        new(new ObservableCollection<IDownloadTaskViewModel>());

    private IObservable<IChangeSet<IDownloadTaskViewModel>> TaskSourceChangeSet { get; }

    public ReadOnlyObservableCollection<IDownloadTaskViewModel> Tasks => _tasksObservable;

    private ReadOnlyObservableCollection<IDataGridColumnFactory<DownloadColumn>>
        _filteredColumns = new(new ObservableCollection<IDataGridColumnFactory<DownloadColumn>>());

    public ReadOnlyObservableCollection<IDataGridColumnFactory<DownloadColumn>> Columns => _filteredColumns;

    [Reactive] public int ActiveDownloadCount { get; set; }

    [Reactive] public bool HasDownloads { get; private set; }

    [Reactive] public int SecondsRemaining { get; private set; }

    public SourceList<IDownloadTaskViewModel> SelectedTasks { get; set; } = new();

    [Reactive] public long DownloadedSizeBytes { get; private set; }

    [Reactive] public long TotalSizeBytes { get; private set; }

    [Reactive] public ICommand ShowCancelDialogCommand { get; set; } = ReactiveCommand.Create(() => { });

    [Reactive] public ICommand SuspendSelectedTasksCommand { get; private set; } = ReactiveCommand.Create(() => { });

    [Reactive] public ICommand ResumeSelectedTasksCommand { get; private set; } = ReactiveCommand.Create(() => { });

    [Reactive] public ICommand SuspendAllTasksCommand { get; private set; } = ReactiveCommand.Create(() => { });
    [Reactive] public ICommand ResumeAllTasksCommand { get; private set; } = ReactiveCommand.Create(() => { });

    // Do nothing for now and keep disabled.
    [Reactive]
    public ICommand ShowSettings { get; private set; } = ReactiveCommand.Create(() => { }, Observable.Return(false));

    [UsedImplicitly]
    public InProgressViewModel(
        IWindowManager windowManager,
        IDownloadService downloadService,
        IOverlayController overlayController) : base(windowManager)
    {
        TaskSourceChangeSet = downloadService.Downloads
            .ToObservableChangeSet()
            .Filter(x => x.PersistentState.Status != DownloadTaskStatus.Completed)
            .Transform(x => (IDownloadTaskViewModel)new DownloadTaskViewModel(x))
            .OnUI();

        Init();

        this.WhenActivated(d =>
        {
            GetWorkspaceController().SetTabTitle(Language.InProgressDownloadsPage_Title, WorkspaceId, PanelId, TabId);

            ShowCancelDialogCommand = ReactiveCommand.Create(async () =>
                {
                    if (SelectedTasks.Items.Any())
                    {
                        var result = await overlayController.ShowCancelDownloadOverlay(SelectedTasks.Items.ToList());
                        if (result)
                            CancelTasks(SelectedTasks.Items);
                    }
                }, Tasks.ToObservableChangeSet()
                    .AutoRefresh(task => task.Status)
                    .Select(_ => Tasks.Any()))
                .DisposeWith(d);
        });
    }

    /// <summary>
    /// For designTime and Testing purposes.
    /// </summary>
    protected InProgressViewModel() : base(new DesignWindowManager())
    {
        TaskSourceChangeSet = DesignTimeDownloadTasks.Connect().OnUI();
        Init();
    }

    /// <summary>
    /// This is used in the constructors to initialize the view model.
    /// </summary>
    private void Init()
    {
        // Make Columns
        var columns =
            new SourceCache<IDataGridColumnFactory<DownloadColumn>, DownloadColumn>(colFactory => colFactory.Type);
        columns.Edit(colUpdater =>
        {
            colUpdater.AddOrUpdate(
                new DataGridColumnDesignFactory<IDownloadNameViewModel, IDownloadTaskViewModel, DownloadColumn>(
                    x => new DownloadNameView()
                    {
                        ViewModel = new DownloadNameViewModel() { Row = x }
                    }, DownloadColumn.DownloadName)
                {
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                });

            colUpdater.AddOrUpdate(
                new DataGridColumnDesignFactory<IDownloadVersionViewModel, IDownloadTaskViewModel, DownloadColumn>(
                    x => new DownloadVersionView()
                    {
                        ViewModel = new DownloadVersionViewModel() { Row = x }
                    }, DownloadColumn.DownloadVersion));

            colUpdater.AddOrUpdate(
                new DataGridColumnDesignFactory<IDownloadGameNameViewModel, IDownloadTaskViewModel, DownloadColumn>(
                    x => new DownloadGameNameView()
                    {
                        ViewModel = new DownloadGameNameViewModel() { Row = x }
                    }, DownloadColumn.DownloadGameName));

            colUpdater.AddOrUpdate(
                new DataGridColumnDesignFactory<IDownloadSizeViewModel, IDownloadTaskViewModel, DownloadColumn>(
                    x => new DownloadSizeView()
                    {
                        ViewModel = new DownloadSizeViewModel() { Row = x }
                    }, DownloadColumn.DownloadSize));

            colUpdater.AddOrUpdate(
                new DataGridColumnDesignFactory<IDownloadStatusViewModel, IDownloadTaskViewModel, DownloadColumn>(
                    x => new DownloadStatusView()
                    {
                        ViewModel = new DownloadStatusViewModel() { Row = x }
                    }, DownloadColumn.DownloadStatus));
        });

        this.WhenActivated(d =>
        {
            TaskSourceChangeSet.Bind(out _tasksObservable)
                .Subscribe()
                .DisposeWith(d);

            columns.Connect()
                .Bind(out _filteredColumns)
                .Subscribe()
                .DisposeWith(d);

            SuspendSelectedTasksCommand = ReactiveCommand.Create(
                    () => { SuspendTasks(SelectedTasks.Items); },
                    SelectedTasks.Connect()
                        .AutoRefresh(task => task.Status)
                        .Select(_ => SelectedTasks.Items.Any(task => task.Status == DownloadTaskStatus.Downloading)))
                .DisposeWith(d);

            ResumeSelectedTasksCommand = ReactiveCommand.Create(
                    () => { ResumeTasks(SelectedTasks.Items); },
                    SelectedTasks.Connect()
                        .AutoRefresh(task => task.Status)
                        .Select(_ => SelectedTasks.Items.Any(task => task.Status == DownloadTaskStatus.Paused)))
                .DisposeWith(d);

            SuspendAllTasksCommand = ReactiveCommand.Create(
                    () => { SuspendTasks(Tasks); },
                    Tasks.ToObservableChangeSet()
                        .AutoRefresh(task => task.Status)
                        .Select(_ => Tasks.Any(task => task.Status == DownloadTaskStatus.Downloading)))
                .DisposeWith(d);

            ResumeAllTasksCommand = ReactiveCommand.Create(
                    () => { ResumeTasks(Tasks); },
                    Tasks.ToObservableChangeSet()
                        .AutoRefresh(task => task.Status)
                        .Select(_ => Tasks.Any(task => task.Status == DownloadTaskStatus.Paused)))
                .DisposeWith(d);

            Tasks.ToObservableChangeSet()
                .AutoRefresh(task => task.Status)
                .Subscribe(_ =>
                {
                    UpdateWindowInfo();
                    ActiveDownloadCount = Tasks.Count(task => task.Status == DownloadTaskStatus.Downloading);
                    HasDownloads = Tasks.Any();
                }).DisposeWith(d);

            // Start updating on the UI thread
            // This is a service to provide polling in case of downloaders that don't have a way of notifying
            // changes in progress (e.g. when file name is resolved, or download progress is made).

            // This is not the only mechanism, a manual refresh also can occur when tasks are added/removed.
            Tick.OnUI()
                .Subscribe(_ => UpdateWindowInfo())
                .DisposeWith(d);
        });
    }

    /// <inheritdoc />
    public virtual void CancelTasks(IEnumerable<IDownloadTaskViewModel> tasks)
    {
        foreach (var task in tasks)
        {
            task.Cancel();
        }
    }

    /// <inheritdoc />
    public virtual void SuspendTasks(IEnumerable<IDownloadTaskViewModel> tasks)
    {
        foreach (var task in tasks)
        {
            if (task.Status == DownloadTaskStatus.Downloading)
                task.Suspend();
        }
    }

    /// <inheritdoc />
    public virtual void ResumeTasks(IEnumerable<IDownloadTaskViewModel> tasks)
    {
        foreach (var task in tasks)
        {
            if (task.Status == DownloadTaskStatus.Paused)
                task.Resume();
        }
    }

    /// <summary>
    /// Updates the window info, providing refresh support for data which might not be natively
    /// notify property changed, or needs to aggregate other data.
    /// </summary>
    private void UpdateWindowInfo()
    {
        // Update Window Info
        UpdateWindowInfoInternal();
    }

    private void UpdateWindowInfoInternal()
    {
        // Calculate Number of Downloaded Bytes.
        long totalDownloadedBytes = 0;
        long totalSizeBytes = 0;

        foreach (var task in Tasks.Where(x => x.Status == DownloadTaskStatus.Downloading))
        {
            totalDownloadedBytes += task.DownloadedBytes;
            totalSizeBytes += task.SizeBytes;
        }

        TotalSizeBytes = totalSizeBytes;
        DownloadedSizeBytes = totalDownloadedBytes;

        // Calculate Remaining Time.
        var throughput = Tasks.Sum(x => x.Throughput);
        var remainingBytes = totalSizeBytes - totalDownloadedBytes;
        SecondsRemaining = throughput < 1.0 ? 0 : (int)(remainingBytes / Math.Max(throughput, 1));
    }
}
