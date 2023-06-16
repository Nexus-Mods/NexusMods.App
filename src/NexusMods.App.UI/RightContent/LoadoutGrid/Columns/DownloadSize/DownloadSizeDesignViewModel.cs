using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadSize;

public class DownloadSizeDesignViewModel : AViewModel<IDownloadSizeViewModel>, IDownloadSizeViewModel, IComparableColumn<IDownloadTaskViewModel>
{
    [Reactive]
    public IDownloadTaskViewModel Row { get; set; } = new DownloadTaskDesignViewModel();

    [Reactive]
    public string Size { get; set; } = "";

    public DownloadSizeDesignViewModel()
    {
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row.SizeBytes, vm => vm.Row.DownloadedBytes)
                .Select(x => FormatSize(x.Item1, x.Item2))
                .BindToUi(this, vm => vm.Size)
                .DisposeWith(d);
        });
    }

    public static string FormatSize(long totalBytes, long usedBytes)
    {
        const float scale = 1000 * 1000 * 1000;
        var usedGB = usedBytes / scale;
        var totalGB = totalBytes / scale;

        return $"{usedGB:F2} GB / {totalGB:F2} GB";
    }
    
    // TODO: It's unclear how to implement this sort currently. For now we sort by downloaded.
    public int Compare(IDownloadTaskViewModel a, IDownloadTaskViewModel b) => a.DownloadedBytes.CompareTo(b.DownloadedBytes);
}

