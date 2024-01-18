using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Binding;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.RightContent.DownloadGrid;
using NexusMods.App.UI.RightContent.DownloadGrid.Columns.DownloadGameName;
using NexusMods.App.UI.RightContent.DownloadGrid.Columns.DownloadStatus;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadName;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadSize;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadVersion;
using NexusMods.Networking.Downloaders.Interfaces;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using DownloadGameNameView = NexusMods.App.UI.RightContent.DownloadGrid.Columns.DownloadGameName.DownloadGameNameView;
using DownloadStatusView = NexusMods.App.UI.RightContent.DownloadGrid.Columns.DownloadStatus.DownloadStatusView;

namespace NexusMods.App.UI.RightContent.Downloads;

public class InProgressCommonViewModel : AViewModel<IInProgressViewModel>, IInProgressViewModel
{
    internal const int PollTimeMilliseconds = 1000;

    protected ReadOnlyObservableCollection<IDownloadTaskViewModel> TasksObservable =
        new(new ObservableCollection<IDownloadTaskViewModel>());

    public ReadOnlyObservableCollection<IDownloadTaskViewModel> Tasks => TasksObservable;

    protected ReadOnlyObservableCollection<IDataGridColumnFactory<DownloadColumn>>
        FilteredColumns = new(new ObservableCollection<IDataGridColumnFactory<DownloadColumn>>());

    public ReadOnlyObservableCollection<IDataGridColumnFactory<DownloadColumn>> Columns => FilteredColumns;

    [Reactive] public int ActiveDownloadCount { get; set; }

    [Reactive] public bool IsRunning { get; set; }

    [Reactive] public int SecondsRemaining { get; set; }

    [Reactive] public IDownloadTaskViewModel? SelectedTask { get; set; }
    public SourceList<IDownloadTaskViewModel> SelectedTasks { get; set; } = new();

    [Reactive] public long DownloadedSizeBytes { get; set; }

    [Reactive] public long TotalSizeBytes { get; set; }

    [Reactive] public ICommand ShowCancelDialog { get; set; }

    [Reactive] public ICommand SuspendCurrentTask { get; set; }

    [Reactive] public ICommand ResumeCurrentTask { get; set; }

    [Reactive] public ICommand SuspendAllTasks { get; set; }
    [Reactive] public ICommand ResumeAllTasks { get; set; }

    public InProgressCommonViewModel()
    {
        ShowCancelDialog = ReactiveCommand.Create(() => { });
        SuspendCurrentTask = ReactiveCommand.Create(() => { });
        SuspendAllTasks = ReactiveCommand.Create(() => { });
        ResumeCurrentTask = ReactiveCommand.Create(() => { });
        ResumeAllTasks = ReactiveCommand.Create(() => { });

        // Make Columns
        var columns = new SourceCache<IDataGridColumnFactory<DownloadColumn>, DownloadColumn>(x => x.Type);
        columns.Edit(x =>
        {
            x.AddOrUpdate(
                new DataGridColumnDesignFactory<IDownloadNameViewModel, IDownloadTaskViewModel, DownloadColumn>(
                    x => new DownloadNameView()
                    {
                        ViewModel = new DownloadNameViewModel() { Row = x }
                    }, DownloadColumn.DownloadName)
                {
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                });

            x.AddOrUpdate(
                new DataGridColumnDesignFactory<IDownloadVersionViewModel, IDownloadTaskViewModel, DownloadColumn>(
                    x => new DownloadVersionView()
                    {
                        ViewModel = new DownloadVersionViewModel() { Row = x }
                    }, DownloadColumn.DownloadVersion));

            x.AddOrUpdate(
                new DataGridColumnDesignFactory<IDownloadGameNameViewModel, IDownloadTaskViewModel, DownloadColumn>(
                    x => new DownloadGameNameView()
                    {
                        ViewModel = new DownloadGameNameViewModel() { Row = x }
                    }, DownloadColumn.DownloadGameName));

            x.AddOrUpdate(
                new DataGridColumnDesignFactory<IDownloadSizeViewModel, IDownloadTaskViewModel, DownloadColumn>(
                    x => new DownloadSizeView()
                    {
                        ViewModel = new DownloadSizeViewModel() { Row = x }
                    }, DownloadColumn.DownloadSize));

            x.AddOrUpdate(
                new DataGridColumnDesignFactory<IDownloadStatusViewModel, IDownloadTaskViewModel, DownloadColumn>(
                    x => new DownloadStatusView()
                    {
                        ViewModel = new DownloadStatusViewModel() { Row = x }
                    }, DownloadColumn.DownloadStatus));
        });

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Tasks)
                .Select(tasks => tasks.ToObservableChangeSet())
                .Subscribe(changeSet =>
                {
                    // Update ViewModel Properties (non-polling) when any task arrives.
                    changeSet.OnUI()
                        .Subscribe(set => UpdateWindowInfo())
                        .DisposeWith(d);
                })
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.Tasks)
                .Select(models => models.ToObservableChangeSet())
                .Subscribe(x =>
                {
                    x.AutoRefresh(task => task.Status)
                        .Subscribe(_ =>
                        {
                            ActiveDownloadCount = Tasks.Count(task => task.Status == DownloadTaskStatus.Downloading);
                            IsRunning = ActiveDownloadCount > 0;
                        }).DisposeWith(d);
                })
                .DisposeWith(d);

            columns.Connect()
                .Bind(out FilteredColumns)
                .Subscribe()
                .DisposeWith(d);

            // Start updating on the UI thread
            // This is a service to provide polling in case of downloaders that don't have a way of notifying
            // changes in progress (e.g. when file name is resolved, or download progress is made).

            // This is not the only mechanism, a manual refresh also can occur when tasks are added/removed.
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(PollTimeMilliseconds);
            timer.Tick += UpdateWindowInfoInternal;
            timer.Start();
            Disposable.Create(timer, (tmr) => tmr.Stop()).DisposeWith(d);

            // This is necessary due to inheritance,
            // WhenActivated gets fired in wrong order and
            // parent classes need to be able to properly subscribe
            // here.
            this.RaisePropertyChanged(nameof(Columns));
        });
    }

    /// <summary>
    /// Updates the window info, providing refresh support for data which might not be natively
    /// notify property changed, or needs to aggregate other data.
    /// </summary>
    protected virtual void UpdateWindowInfo()
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

    private void UpdateWindowInfoInternal(object? sender, object? state) => UpdateWindowInfo();
}
