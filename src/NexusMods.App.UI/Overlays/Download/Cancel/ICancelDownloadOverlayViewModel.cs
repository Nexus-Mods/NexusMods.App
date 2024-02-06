using NexusMods.App.UI.Overlays.Generic.MessageBox.OkCancel;
using NexusMods.App.UI.Pages.Downloads.ViewModels;

namespace NexusMods.App.UI.Overlays.Download.Cancel;

/// <summary>
/// ViewModel for <see cref="CancelDownloadOverlayView"/>.
/// </summary>
public interface ICancelDownloadOverlayViewModel : IMessageBoxOkCancelViewModel
{
    /// <summary>
    /// The task that we will request if it's means to be cancelled.
    /// </summary>
    public IReadOnlyList<IDownloadTaskViewModel> DownloadTasks { get; }
}
