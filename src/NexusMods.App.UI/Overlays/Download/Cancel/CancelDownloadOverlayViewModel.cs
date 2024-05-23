using NexusMods.App.UI.Pages.Downloads.ViewModels;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays.Download.Cancel;

public class CancelDownloadOverlayViewModel : AOverlayViewModel<ICancelDownloadOverlayViewModel, bool>, ICancelDownloadOverlayViewModel
{
    public IReadOnlyList<IDownloadTaskViewModel> DownloadTasks { get; } = new List<IDownloadTaskViewModel>();
    
    // For design.
    public CancelDownloadOverlayViewModel() { }

    // For runtime.
    public CancelDownloadOverlayViewModel(IReadOnlyList<IDownloadTaskViewModel> tasks) { DownloadTasks = tasks; }
}
