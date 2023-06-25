using NexusMods.App.UI.RightContent.Downloads.ViewModels;

namespace NexusMods.App.UI.Overlays.Download.Cancel;

public class CancelDownloadOverlayViewModel : AViewModel<ICancelDownloadOverlayViewModel>, ICancelDownloadOverlayViewModel
{
    public IDownloadTaskViewModel DownloadTask { get; } = new DownloadTaskDesignViewModel();

    public Action Ok => () =>
    {
        DownloadTask.Cancel();
        IsActive = false;
    };
    
    public bool IsActive { get; set; } = true;
    
    // For design.
    public CancelDownloadOverlayViewModel() { }

    // For runtime.
    public CancelDownloadOverlayViewModel(IDownloadTaskViewModel task) { DownloadTask = task; }
}
