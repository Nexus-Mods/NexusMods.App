using Avalonia.ReactiveUI;
using NexusMods.App.UI.Resources;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.Download.Cancel;

public partial class CancelDownloadOverlayView : ReactiveUserControl<ICancelDownloadOverlayViewModel>
{
    public CancelDownloadOverlayView()
    {
        InitializeComponent();
        this.WhenActivated(_ =>
        {
            OkCancelView.Description =
                string.Format(Language.CancelDownloadOverlayView_Description_download_will_be_cancelled,
                    ViewModel?.DownloadTask.Name);
        });
    }
}
