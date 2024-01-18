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

            OkCancelView.Description = ViewModel?.DownloadTasks.Count() == 1 ?
                string.Format(Language.CancelDownloadOverlayView_Description_download_will_be_cancelled,
                    ViewModel?.DownloadTasks.First().Name + " " +
                    Language.CancelDownloadOverlayView_Description__Download) :
                string.Format(Language.CancelDownloadOverlayView_Description_download_will_be_cancelled,
                    ViewModel?.DownloadTasks.Count() + " " +
                    Language.CancelDownloadOverlayView_Description_downloads );
        });
    }
}
