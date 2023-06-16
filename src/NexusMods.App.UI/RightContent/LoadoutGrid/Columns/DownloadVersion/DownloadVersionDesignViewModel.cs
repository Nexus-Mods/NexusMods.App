using System.Reactive.Disposables;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadVersion;

public class DownloadVersionDesignViewModel : AViewModel<IDownloadVersionViewModel>, IDownloadVersionViewModel
{
    [Reactive]
    public IDownloadTaskViewModel Row { get; set; } = new DownloadTaskDesignViewModel();

    [Reactive]
    public string Version { get; set; } = "";

    public DownloadVersionDesignViewModel()
    {
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row.Version)
                .BindToUi(this, vm => vm.Version)
                .DisposeWith(d);
        });
    }
}
