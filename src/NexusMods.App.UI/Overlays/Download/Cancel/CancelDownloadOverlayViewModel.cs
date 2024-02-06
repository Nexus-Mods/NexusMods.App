using NexusMods.App.UI.Pages.Downloads.ViewModels;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays.Download.Cancel;

public class CancelDownloadOverlayViewModel : AViewModel<ICancelDownloadOverlayViewModel>, ICancelDownloadOverlayViewModel
{
    public IReadOnlyList<IDownloadTaskViewModel> DownloadTasks { get; } = new List<IDownloadTaskViewModel>();

    [Reactive]
    public bool DialogResult { get; set; }

    [Reactive]
    public bool IsActive { get; set; } = true;

    // For design.
    public CancelDownloadOverlayViewModel() { }

    // For runtime.
    public CancelDownloadOverlayViewModel(IReadOnlyList<IDownloadTaskViewModel> tasks) { DownloadTasks = tasks; }
}
