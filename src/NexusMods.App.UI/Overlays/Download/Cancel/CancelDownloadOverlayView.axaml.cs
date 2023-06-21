using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.Download.Cancel;

public partial class CancelDownloadOverlayView : ReactiveUserControl<ICancelDownloadOverlayViewModel>
{
    public CancelDownloadOverlayView()
    {
        InitializeComponent();
        this.WhenActivated(_ =>
        {
            OkCancelView.Description = $"\"{ViewModel?.DownloadTask.Name}\" download will be cancelled and the files will be deleted.";
        });
    }
}

