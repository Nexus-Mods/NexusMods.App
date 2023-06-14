using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadSize;

public class DownloadSizeViewModel : AViewModel<IDownloadSizeViewModel>, IDownloadSizeViewModel
{
    [Reactive]
    public IDownloadTaskViewModel Row { get; set; } = new DownloadTaskDesignViewModel();

    [Reactive]
    public string Size { get; set; } = "";

    public DownloadSizeViewModel()
    {
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row.SizeBytes, vm => vm.Row.DownloadedBytes)
                .Select(x => DownloadSizeDesignViewModel.FormatSize(x.Item1, x.Item2))
                .BindToUi(this, vm => vm.Size)
                .DisposeWith(d);
        });
    }
}

