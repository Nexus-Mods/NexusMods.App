using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Helpers;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadSize;

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
            this.WhenAnyValue(vm => vm.Row.DownloadedBytes, vm => vm.Row.SizeBytes)
                .Select(x => StringFormatters.ToSizeString(x.Item1, x.Item2))
                .BindToUi(this, vm => vm.Size)
                .DisposeWith(d);
        });
    }

    // TODO: It's unclear how to implement this sort currently. For now we sort by downloaded.
    public int Compare(IDownloadTaskViewModel a, IDownloadTaskViewModel b) => a.DownloadedBytes.CompareTo(b.DownloadedBytes);
}

