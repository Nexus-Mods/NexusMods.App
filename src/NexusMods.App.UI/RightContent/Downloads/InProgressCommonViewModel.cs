using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Threading;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns;
using NexusMods.Networking.Downloaders.Interfaces;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.Downloads;

public class InProgressCommonViewModel : AViewModel<IInProgressViewModel>, IInProgressViewModel
{
    protected ReadOnlyObservableCollection<IDownloadTaskViewModel> TasksObservable = new(new ObservableCollection<IDownloadTaskViewModel>());
    
    public ReadOnlyObservableCollection<IDownloadTaskViewModel> Tasks => TasksObservable;
    
    protected ReadOnlyObservableCollection<IDataGridColumnFactory>
        FilteredColumns = new(new ObservableCollection<IDataGridColumnFactory>());
    
    public ReadOnlyObservableCollection<IDataGridColumnFactory> Columns => FilteredColumns;
    
    [Reactive]
    public bool IsRunning { get; set; }
    
    [Reactive]
    public int SecondsRemaining { get; set; }

    [Reactive]
    public long DownloadedSizeBytes { get; set; }
    
    [Reactive]
    public long TotalSizeBytes { get; set; }

    private readonly CancellationTokenSource _backgroundUpdateToken = new();
    private Timer _timer = null!;
    
    public InProgressCommonViewModel()
    {
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Tasks)
                .Select(task => task.Any(x => !(x.Status is DownloadTaskStatus.Idle or DownloadTaskStatus.Paused)))
                .BindToUi(this, vm => vm.IsRunning)
                .DisposeWith(d);

            _timer = new Timer(UpdateWindowInfoInternal, null, 0, 1000);
            _backgroundUpdateToken.DisposeWith(d);
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

        foreach (var task in Tasks)
        {
            totalDownloadedBytes += task.DownloadedBytes;
            totalSizeBytes += task.SizeBytes;
        }

        TotalSizeBytes = totalSizeBytes;
        DownloadedSizeBytes = totalDownloadedBytes;
        
        // Calculate Remaining Time.
        var throughput = Tasks.Sum(x => x.Throughput);
        var remainingBytes = totalSizeBytes - totalDownloadedBytes;
        SecondsRemaining = (int)(remainingBytes / throughput);
    }
    
    private void UpdateWindowInfoInternal(object? state)
    {
        while (!_backgroundUpdateToken.IsCancellationRequested)
        {
            // We need to stop timer while running task, for sake of debuggers or unexpectedly long callback.
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                try { UpdateWindowInfo(); }
                finally { _timer.Change(1000, 1000); }
            });
        }
    }
}
