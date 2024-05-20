using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Controls.DownloadGrid;
using NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadGameName;
using NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadName;
using NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadSize;
using NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadStatus;
using NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadVersion;
using NexusMods.App.UI.Helpers;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Pages.Downloads.ViewModels;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Paths;
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
    protected readonly SourceCache<IDownloadTaskViewModel, EntityId> DesignTimeDownloadTasks = new(x => x.TaskId);

    private ReadOnlyObservableCollection<IDownloadTaskViewModel> _inProgressTasksObservable = new([]);
    
    private ReadOnlyObservableCollection<IDownloadTaskViewModel> _completedTasksObservable = new([]);
    
    private IObservable<IChangeSet<IDownloadTaskViewModel, EntityId>> InProgressTaskChangeSet { get; }
    private IObservable<IChangeSet<IDownloadTaskViewModel, EntityId>> CompletedTaskChangeSet { get; }

    public ReadOnlyObservableCollection<IDownloadTaskViewModel> InProgressTasks => _inProgressTasksObservable;
    public ReadOnlyObservableCollection<IDownloadTaskViewModel> CompletedTasks => _completedTasksObservable;

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

    private readonly ObservableCollectionExtended<DateTimePoint> _throughputValues = [];
    public ReadOnlyObservableCollection<ISeries> Series { get; } = ReadOnlyObservableCollection<ISeries>.Empty;

    private readonly ObservableCollectionExtended<double> _customSeparators = [0];

    public Axis[] YAxes { get; } = [];

    public Axis[] XAxes { get; } =
    [
        new DateTimeAxis(TimeSpan.FromSeconds(1), static date => date.ToString("HH:mm:ss"))
        {
            AnimationsSpeed = TimeSpan.FromMilliseconds(0),
            LabelsPaint = null,
        },
    ];

    [UsedImplicitly]
    public InProgressViewModel(
        IWindowManager windowManager,
        IDownloadService downloadService,
        IOverlayController overlayController) : base(windowManager)
    {
        Series = new ReadOnlyObservableCollection<ISeries>([
            new LineSeries<DateTimePoint>
            {
                Values = _throughputValues,
                Fill = null,
                GeometryFill = null,
                GeometryStroke = null,
            },
        ]);

        YAxes =
        [
            new Axis
            {
                MinLimit = 0,
                CustomSeparators = _customSeparators,
                Labeler = static value => StringFormatters.ToThroughputString((long)value, TimeSpan.FromSeconds(1)),
            },
        ];

        TabTitle = Language.InProgressDownloadsPage_Title;
        TabIcon = IconValues.Downloading;

        var tasksChangeSet = downloadService.Downloads
            .ToObservableChangeSet(x => x.PersistentState.Id)
            .Transform(x =>
                {
                    var vm = new DownloadTaskViewModel(x);
                    vm.Activator.Activate();
                    return (IDownloadTaskViewModel)vm;
                }
            );
        
        InProgressTaskChangeSet = tasksChangeSet
            .FilterOnObservable((item, key) =>
                {
                    return item.WhenAnyValue(v => v.Status)
                        .Select(s => s != DownloadTaskStatus.Cancelled && s != DownloadTaskStatus.Completed);
                }
            )
            .DisposeMany()
            .OnUI();
        
        CompletedTaskChangeSet = tasksChangeSet
            .FilterOnObservable((item, key) =>
                {
                    return item.WhenAnyValue(v => v.Status)
                        .CombineLatest(item.WhenAnyValue(v => v.IsHidden))
                        .Select(_ => item is { Status: DownloadTaskStatus.Completed, IsHidden: false });
                }
            )
            .DisposeMany()
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
                }, InProgressTasks.ToObservableChangeSet()
                    .AutoRefresh(task => task.Status)
                    .Select(_ => InProgressTasks.Any()))
                .DisposeWith(d);
        });
    }

    /// <summary>
    /// For designTime and Testing purposes.
    /// </summary>
    protected InProgressViewModel() : base(new DesignWindowManager())
    {
        InProgressTaskChangeSet = DesignTimeDownloadTasks.Connect().OnUI();
        CompletedTaskChangeSet = DesignTimeDownloadTasks.Connect().OnUI();
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
            InProgressTaskChangeSet
                .Bind(out _inProgressTasksObservable)
                .Subscribe()
                .DisposeWith(d);
            
            CompletedTaskChangeSet
                .Bind(out _completedTasksObservable)
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
                    () => { SuspendTasks(InProgressTasks); },
                    InProgressTasks.ToObservableChangeSet()
                        .AutoRefresh(task => task.Status)
                        .Select(_ => InProgressTasks.Any(task => task.Status == DownloadTaskStatus.Downloading)))
                .DisposeWith(d);

            ResumeAllTasksCommand = ReactiveCommand.Create(
                    () => { ResumeTasks(InProgressTasks); },
                    InProgressTasks.ToObservableChangeSet()
                        .AutoRefresh(task => task.Status)
                        .Select(_ => InProgressTasks.Any(task => task.Status == DownloadTaskStatus.Paused)))
                .DisposeWith(d);

            InProgressTasks.ToObservableChangeSet()
                .AutoRefresh(task => task.Status)
                .Subscribe(_ =>
                {
                    UpdateWindowInfo();
                    ActiveDownloadCount = InProgressTasks.Count(task => task.Status == DownloadTaskStatus.Downloading);
                    HasDownloads = InProgressTasks.Any();
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
    
    private async Task HideTasks(IEnumerable<IDownloadTaskViewModel> downloads)
    {
        var dls = downloads
            .Where(x => x.Status == DownloadTaskStatus.Completed)
            .ToArray();
        if (dls.Length == 0)
            return;
        
        foreach (var dl in dls)
        {
            // dl.Task.PersistentState.Remap<CompletedDownloadState.Model>().IsHidden = true;
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
        var activeTasks = InProgressTasks.Where(x => x.Status == DownloadTaskStatus.Downloading).ToArray();

        foreach (var task in activeTasks)
        {
            totalDownloadedBytes += task.DownloadedBytes;
            totalSizeBytes += task.SizeBytes;
        }

        TotalSizeBytes = totalSizeBytes;
        DownloadedSizeBytes = totalDownloadedBytes;

        // Calculate Remaining Time.
        var throughput = activeTasks.Sum(x => x.Throughput);
        var remainingBytes = totalSizeBytes - totalDownloadedBytes;
        SecondsRemaining = throughput < 1.0 ? 0 : (int)(remainingBytes / Math.Max(throughput, 1));

        if (_throughputValues.Count > 120)
        {
            _throughputValues.RemoveRange(0, count: 60);
        }

        throughput = totalSizeBytes > 0 ? throughput : 0;
        if (throughput > _customSeparators.Last())
        {
            _customSeparators.Add(FirstMultiple(throughput));
        }

        _throughputValues.Add(new DateTimePoint(DateTime.Now, throughput));
    }

    private static double FirstMultiple(long value)
    {
        const int step = 5;
        var start = (long)Size.MB.Value * step;

        var res = start;

        while (res <= value)
        {
            res += start;
        }

        return res;
    }
}
