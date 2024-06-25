using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using DynamicData;
using DynamicData.Binding;
using DynamicData.Kernel;
using JetBrains.Annotations;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Controls.DownloadGrid;
using NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadGameName;
using NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadName;
using NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadSize;
using NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadStatus;
using NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadVersion;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Helpers;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.Download.Cancel;
using NexusMods.App.UI.Pages.Downloads.ViewModels;
using NexusMods.App.UI.Pages.ModLibrary;
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

    private IDownloadService _downloadService;
    
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

    public SourceList<IDownloadTaskViewModel> SelectedInProgressTasks { get; set; } = new();
    public SourceList<IDownloadTaskViewModel> SelectedCompletedTasks { get; set; } = new();
    
    private IObservable<IChangeSet<IDownloadTaskViewModel>> SelectedInprogressTaskChangeSet {get; set;}
    private IObservable<IChangeSet<IDownloadTaskViewModel>> SelectedCompletedTaskChangeSet {get; set;}

    
    [Reactive] public long DownloadedSizeBytes { get; private set; }

    [Reactive] public long TotalSizeBytes { get; private set; }

    [Reactive] public ReactiveCommand<Unit,Unit> ShowCancelDialogCommand { get; set; } = ReactiveCommand.Create(() => { });

    [Reactive] public ReactiveCommand<Unit,Unit> SuspendSelectedTasksCommand { get; private set; } = ReactiveCommand.Create(() => { });

    [Reactive] public ReactiveCommand<Unit,Unit> ResumeSelectedTasksCommand { get; private set; } = ReactiveCommand.Create(() => { });

    [Reactive] public ReactiveCommand<Unit,Unit> SuspendAllTasksCommand { get; private set; } = ReactiveCommand.Create(() => { });
    [Reactive] public ReactiveCommand<Unit,Unit> ResumeAllTasksCommand { get; private set; } = ReactiveCommand.Create(() => { });
    [Reactive] public ReactiveCommand<Unit, Unit> HideSelectedCommand { get; private set; } = ReactiveCommand.Create(() => { });
    [Reactive] public ReactiveCommand<Unit, Unit> HideAllCommand { get; private set; } = ReactiveCommand.Create(() => { });

    private readonly ObservableCollectionExtended<DateTimePoint> _throughputValues = [];
    private readonly ISeries _lineSeries;
    public ReadOnlyObservableCollection<ISeries> Series { get; }

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
        IConnection conn,
        IOverlayController overlayController) : base(windowManager)
    {
        _downloadService = downloadService;
        _lineSeries = new LineSeries<DateTimePoint>
        {
            Values = _throughputValues,
            Fill = null,
            GeometryFill = null,
            GeometryStroke = null,
        };

        Series = new ReadOnlyObservableCollection<ISeries>([_lineSeries]);

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

        var tasksChangeSet = _downloadService.Downloads
            .ToObservableChangeSet(x => x.PersistentState.Id);
        
        InProgressTaskChangeSet = tasksChangeSet
            .Transform(x =>
                {
                    var vm = new DownloadTaskViewModel(x);
                    vm.Activator.Activate();
                    return (IDownloadTaskViewModel)vm;
                }
            )
            .FilterOnObservable((item, key) =>
                {
                    return item.WhenAnyValue(v => v.Status)
                        .Select(s => s != DownloadTaskStatus.Cancelled && s != DownloadTaskStatus.Completed);
                }
            )
            .AutoRefreshOnObservable(task =>
                {
                    return task.WhenAnyValue(x => x.Status);
                }
            )
            .DisposeMany()
            .OnUI();
            

        CompletedTaskChangeSet = tasksChangeSet
            .Transform(x =>
                {
                    var vm = new DownloadTaskViewModel(x);
                    vm.HideCommand = ReactiveCommand.CreateFromTask(async () => await HideTasks(true, [vm]));
                    vm.ViewInLibraryCommand = ReactiveCommand.Create<NavigationInformation>((navInfo) =>
                    {
                        var controller = GetWorkspaceController();
                        var workspaces = controller
                            .AllWorkspaces
                            .Where(w =>
                                {
                                    if (w.Context is not LoadoutContext loadoutContext)
                                    {
                                        return false;
                                    }
                                    var loadout = conn.Db.Get<Loadout.Model>(loadoutContext.LoadoutId.Value);
                                    return loadout.IsVisible() && loadout.Installation.Game.Domain.Equals(vm.Game);
                                }
                            )
                            .Select(w => (w.Id, Context: (LoadoutContext)w.Context)).ToArray();
                        
                        if (workspaces.Length == 0)
                            return;
                        
                        var workspace = workspaces[0];

                        var pageData = new PageData
                        {
                            FactoryId = FileOriginsPageFactory.StaticId,
                            Context = new FileOriginsPageContext { LoadoutId = workspace.Context.LoadoutId },
                        };
                        var behavior = GetWorkspaceController().GetOpenPageBehavior(pageData, navInfo, Optional<PageIdBundle>.None);
                        
                        controller.OpenPage(workspace.Id, pageData, behavior);
                        controller.ChangeActiveWorkspace(workspace.Id);
                    });
                    vm.Activator.Activate();
                    return (IDownloadTaskViewModel)vm;
                }
            )
            .FilterOnObservable((item, key) =>
                {
                    return item.WhenAnyValue(v => v.Status)
                        .CombineLatest(item.WhenAnyValue(v => v.IsHidden))
                        .Select(_ => item is { Status: DownloadTaskStatus.Completed, IsHidden: false });
                }
            )
            .DisposeMany()
            .OnUI();
        
        SelectedInprogressTaskChangeSet = SelectedInProgressTasks.Connect()
            .AutoRefreshOnObservable(item =>
                {
                    return item.WhenAnyValue(x => x.Status);
                }
            )
            .OnUI();
        
        SelectedCompletedTaskChangeSet = SelectedCompletedTasks.Connect()
            .OnUI();

        Init();

        this.WhenActivated(d =>
        {
            GetWorkspaceController().SetTabTitle(Language.InProgressDownloadsPage_Title, WorkspaceId, PanelId, TabId);

            ShowCancelDialogCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    if (SelectedInProgressTasks.Items.Any())
                    {
                        var newCancelDialog = new CancelDownloadOverlayViewModel(SelectedInProgressTasks.Items.ToList());
                        var result = await overlayController.EnqueueAndWait(newCancelDialog);
                        if (result)
                            await CancelTasks(SelectedInProgressTasks.Items);
                    }
                }, SelectedInprogressTaskChangeSet
                .Select(_ => SelectedInProgressTasks.Items.Any()))
                .DisposeWith(d);
        });
    }

    /// <summary>
    /// For designTime and Testing purposes.
    /// </summary>
    protected InProgressViewModel() : base(new DesignWindowManager())
    {
        _lineSeries = new LineSeries<DateTimePoint>();
        Series = ReadOnlyObservableCollection<ISeries>.Empty;

        InProgressTaskChangeSet = DesignTimeDownloadTasks.Connect().OnUI();
        CompletedTaskChangeSet = DesignTimeDownloadTasks.Connect().OnUI();
        SelectedInprogressTaskChangeSet = SelectedInProgressTasks.Connect().OnUI();
        SelectedCompletedTaskChangeSet = SelectedCompletedTasks.Connect().OnUI();
        _downloadService = null!;
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

            SuspendSelectedTasksCommand = ReactiveCommand.CreateFromTask(
                    async () => { await SuspendTasks(SelectedInProgressTasks.Items); },
                    SelectedInprogressTaskChangeSet
                        .Select(_ => SelectedInProgressTasks.Items.Any(task => task.Status == DownloadTaskStatus.Downloading)))
                .DisposeWith(d);

            ResumeSelectedTasksCommand = ReactiveCommand.CreateFromTask(
                    async () => { await ResumeTasks(SelectedInProgressTasks.Items); },
                    SelectedInprogressTaskChangeSet
                        .Select(_ => SelectedInProgressTasks.Items.Any(task => task.Status == DownloadTaskStatus.Paused)))
                .DisposeWith(d);

            SuspendAllTasksCommand = ReactiveCommand.CreateFromTask(
                    async () => { await SuspendTasks(InProgressTasks); },
                    InProgressTaskChangeSet
                        .Select(_ => InProgressTasks.Any(task => task.Status == DownloadTaskStatus.Downloading)))
                .DisposeWith(d);

            ResumeAllTasksCommand = ReactiveCommand.CreateFromTask(
                    async () => { await ResumeTasks(InProgressTasks); },
                    InProgressTaskChangeSet
                        .Select(_ => InProgressTasks.Any(task => task.Status == DownloadTaskStatus.Paused)))
                .DisposeWith(d);
            
            HideSelectedCommand = ReactiveCommand.CreateFromTask(async () =>
                    {
                        await HideTasks(true, SelectedCompletedTasks.Items);
                    },
                    SelectedCompletedTaskChangeSet.Select(_ => SelectedCompletedTasks.Items.Any()))
                .DisposeWith(d);
            
            HideAllCommand = ReactiveCommand.CreateFromTask(async () =>
                    {
                        await HideTasks(true, CompletedTasks);
                    },
                    CompletedTasks.ToObservableChangeSet().Select(_ => CompletedTasks.Any()))
                .DisposeWith(d);
            
            InProgressTasks.ToObservableChangeSet()
                .AutoRefreshOnObservable(item =>
                    {
                        return item.WhenAnyValue(x => x.Status);
                    })
                .OnUI()
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

    private Task CancelTasks(IEnumerable<IDownloadTaskViewModel> tasks)
    {
        return Task.WhenAll(tasks.Select(t => t.Cancel()));
    }

    private Task SuspendTasks(IEnumerable<IDownloadTaskViewModel> tasks)
    {
        return Task.WhenAll(
            tasks.Where(t => t.Status == DownloadTaskStatus.Downloading)
                .Select(t => t.Suspend())
        );
    }

    private Task ResumeTasks(IEnumerable<IDownloadTaskViewModel> tasks)
    {
        return Task.WhenAll(
            tasks.Where(t => t.Status == DownloadTaskStatus.Paused)
                .Select(t => t.Resume())
        );
    }
    
    private async Task HideTasks(bool hide, IEnumerable<IDownloadTaskViewModel> downloads)
    {
        var dls = downloads
            .Where(x => x.Status == DownloadTaskStatus.Completed)
            .ToArray();
            
        if (dls.Length == 0)
            return;
        await _downloadService.SetIsHidden(hide, dls.Select(x => x.DlTask).ToArray());
        
        // Need to manually update value to refresh the UI, we don't listen to db for changes on IsHidden
        foreach (var download in dls)
        {
            download.IsHidden = hide;
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

        // Graph
        if (_throughputValues.Count > 120)
        {
            _throughputValues.RemoveRange(0, count: 60);
        }

        throughput = totalSizeBytes > 0 ? throughput : 0;
        if (throughput > _customSeparators.Last())
        {
            _customSeparators.Add(FirstMultiple(throughput));
        }

        const int delay = 5;
        _lineSeries.IsVisible = _throughputValues
            .TakeLast(Math.Min(delay, _throughputValues.Count))
            .Sum(x => x.Value!.Value) > 0.0;

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
