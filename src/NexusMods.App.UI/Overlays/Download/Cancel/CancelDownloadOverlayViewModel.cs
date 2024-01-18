using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays.Download.Cancel;

public class CancelDownloadOverlayViewModel : AViewModel<ICancelDownloadOverlayViewModel>, ICancelDownloadOverlayViewModel
{
    public IEnumerable<IDownloadTaskViewModel> DownloadTasks { get; } = Enumerable.Empty<IDownloadTaskViewModel>();

    [Reactive]
    public bool DialogResult { get; set; }

    [Reactive]
    public bool IsActive { get; set; } = true;

    // For design.
    public CancelDownloadOverlayViewModel() { }

    // For runtime.
    public CancelDownloadOverlayViewModel(IEnumerable<IDownloadTaskViewModel> tasks) { DownloadTasks = tasks; }
}
