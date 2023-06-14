using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using NexusMods.Networking.Downloaders.Interfaces;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadPausePlay;

public class DownloadPausePlayDesignViewModel : AViewModel<IDownloadPausePlayViewModel>, IDownloadPausePlayViewModel
{
    [Reactive]
    public IDownloadTaskViewModel Row { get; set; } = new DownloadTaskDesignViewModel();
    
    [Reactive]
    public bool CanPause { get; set; }

    public DownloadPausePlayDesignViewModel()
    {
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row.Status)
                .Select(status => !(status is DownloadTaskStatus.Idle or DownloadTaskStatus.Paused))
                .BindToUi(this, vm => vm.CanPause)
                .DisposeWith(d);
        });
    }

}
