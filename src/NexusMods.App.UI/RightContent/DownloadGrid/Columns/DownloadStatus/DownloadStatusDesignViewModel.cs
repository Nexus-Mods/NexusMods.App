using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using NexusMods.Networking.Downloaders.Interfaces;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.DownloadGrid.Columns.DownloadStatus;

public class DownloadStatusDesignViewModel : AViewModel<IDownloadStatusViewModel>, IDownloadStatusViewModel, IComparableColumn<IDownloadTaskViewModel>
{
    [Reactive]
    public IDownloadTaskViewModel Row { get; set; } = new DownloadTaskDesignViewModel();

    [Reactive]
    public string Text { get; set; } = Language.DownloadStatusDesignViewModel_Text_Queued_0_;

    [Reactive]
    public float CurrentValue { get; set; }

    [Reactive]
    public bool IsRunning { get; set; }

    [Reactive]
    public bool CanPause { get; set; }

    [Reactive]
    public ICommand PauseOrResume { get; set; }

    public DownloadStatusDesignViewModel()
    {
        PauseOrResume = ReactiveCommand.Create(() => {
            if (Row.Status != DownloadTaskStatus.Paused)
                Row.Suspend();
            else if (Row.Status != DownloadTaskStatus.Downloading || Row.Status != DownloadTaskStatus.Idle)
                Row.Resume();
        });

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

            this.WhenAnyValue(vm => vm.Row.Status)
                .Select(status => !(status is DownloadTaskStatus.Idle or DownloadTaskStatus.Paused))
                .BindToUi(this, vm => vm.CanPause)
                .DisposeWith(d);
        });
    }

    public static string FormatStatus(DownloadTaskStatus state, long totalBytes, long usedBytes)
    {
        var percent = Math.Round((usedBytes / (float)Math.Max(1, totalBytes) * 100));
        var status = state switch
        {
            DownloadTaskStatus.Idle => Language.DownloadStatusDesignViewModel_FormatStatus_Queued,
            DownloadTaskStatus.Paused => Language.DownloadStatusDesignViewModel_FormatStatus_Paused,
            DownloadTaskStatus.Downloading => Language.DownloadStatusDesignViewModel_FormatStatus_Downloading,
            DownloadTaskStatus.Installing => Language.DownloadStatusDesignViewModel_FormatStatus_Installing,
            DownloadTaskStatus.Completed => Language.DownloadStatusDesignViewModel_FormatStatus_Complete,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

        return $"{status} {percent}%";
    }

    public int Compare(IDownloadTaskViewModel a, IDownloadTaskViewModel b)
    {
        var decA = a.DownloadedBytes / (float)Math.Max(1, a.SizeBytes);
        var decB = b.DownloadedBytes / (float)Math.Max(1, b.SizeBytes);
        return decA.CompareTo(decB);
    }
}
