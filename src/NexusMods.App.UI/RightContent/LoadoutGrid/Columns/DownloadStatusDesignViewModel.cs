using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using NexusMods.Networking.Downloaders.Interfaces;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public class DownloadStatusDesignViewModel : AViewModel<IDownloadStatusViewModel>, IDownloadStatusViewModel
{
    [Reactive]
    public IDownloadTaskViewModel Row { get; set; } = new DownloadTaskDesignViewModel();
    
    [Reactive]
    public string Text { get; set; } = "Queued 0%";
    
    [Reactive]
    public float CurrentValue { get; set; }

    [Reactive]
    public bool IsRunning { get; set; }

    public DownloadStatusDesignViewModel()
    {
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row.Status, vm => vm.Row.SizeBytes, vm => vm.Row.DownloadedBytes)
                .Select(x => FormatStatus(x.Item1, x.Item2, x.Item3))
                .BindToUi(this, vm => vm.Text)
                .DisposeWith(d);
            
            this.WhenAnyValue(vm => vm.Row.Status)
                .Select(status => !(status is DownloadTaskStatus.Idle or DownloadTaskStatus.Paused))
                .BindToUi(this, vm => vm.IsRunning)
                .DisposeWith(d);
            
            this.WhenAnyValue(vm => vm.Row.DownloadedBytes, vm => vm.Row.SizeBytes)
                .Select(x => x.Item1 / (float)Math.Max(1, x.Item2))
                .BindToUi(this, vm => vm.CurrentValue)
                .DisposeWith(d);
        });
    }

    public static string FormatStatus(DownloadTaskStatus state, long totalBytes, long usedBytes)
    {
        var percent = Math.Round((usedBytes / (float)Math.Max(1, totalBytes) * 100));
        var status = state switch
        {
            DownloadTaskStatus.Idle => "Queued",
            DownloadTaskStatus.Paused => "Paused",
            DownloadTaskStatus.Downloading => "Downloading",
            DownloadTaskStatus.Installing => "Installing",
            DownloadTaskStatus.Completed => "Complete",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
        
        return $"{status} {percent}%";
    }
}
