using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays.Download.Cancel;

public class CancelDownloadOverlayViewModel : AViewModel<ICancelDownloadOverlayViewModel>, ICancelDownloadOverlayViewModel
{
    public IDownloadTaskViewModel DownloadTask { get; } = new DownloadTaskDesignViewModel();

    [Reactive]
    public bool DialogResult { get; set; }

    [Reactive]
    public bool IsActive { get; set; } = true;
    
    // For design.
    public CancelDownloadOverlayViewModel() { }

    // For runtime.
    public CancelDownloadOverlayViewModel(IDownloadTaskViewModel task) { DownloadTask = task; }
}
