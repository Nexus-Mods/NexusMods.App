using System.Reactive.Disposables;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadName;

public class DownloadNameDesignViewModel : AViewModel<IDownloadNameViewModel>, IDownloadNameViewModel
{
    [Reactive]
    public IDownloadTaskViewModel Row { get; set; } = new DownloadTaskDesignViewModel();

    [Reactive]
    public string Game { get; set; } = "";

    public DownloadNameDesignViewModel()
    {
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row.Game)
                .BindToUi(this, vm => vm.Game)
                .DisposeWith(d);
        });
    }
}
