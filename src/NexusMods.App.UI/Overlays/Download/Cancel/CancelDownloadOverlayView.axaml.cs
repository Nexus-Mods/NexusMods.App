using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Resources;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.Download.Cancel;

public partial class CancelDownloadOverlayView : ReactiveUserControl<ICancelDownloadOverlayViewModel>
{
    public CancelDownloadOverlayView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(view => view.ViewModel)
                .WhereNotNull()
                .Do(PopulateFromViewModel)
                .Subscribe()
                .DisposeWith(d);
        });
    }

    void PopulateFromViewModel(ICancelDownloadOverlayViewModel vm)
    {
        if (vm.DownloadTasks.Count == 1)
        {
            OkCancelView.Description = string.Format(
                Language.CancelDownloadOverlayView_Description_download_will_be_cancelled,
                vm.DownloadTasks[0].Name + " " + Language.CancelDownloadOverlayView_Description__Download);
        }
        else
        {
            OkCancelView.Description = string.Format(
                Language.CancelDownloadOverlayView_Description_download_will_be_cancelled,
                vm.DownloadTasks.Count + " " + Language.CancelDownloadOverlayView_Description_downloads);
        }
    }
}
