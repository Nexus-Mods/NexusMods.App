using NexusMods.App.UI.RightContent.Downloads.ViewModels;

namespace NexusMods.App.UI.Overlays.Download.Cancel;

public class CancelDownloadOverlayViewModel : AViewModel<ICancelDownloadOverlayViewModel>, ICancelDownloadOverlayViewModel
{
    public IDownloadTaskViewModel DownloadTask { get; } = new DownloadTaskDesignViewModel();

    public Action Ok => DownloadTask.Cancel;

    public Action Cancel { get; set; }
    
    // For design.
    public CancelDownloadOverlayViewModel() => Cancel = () => { };

    // For runtime.
    public CancelDownloadOverlayViewModel(IDownloadTaskViewModel task, Action cancel)
    {
        Cancel = cancel;
        DownloadTask = task;
    }
}
