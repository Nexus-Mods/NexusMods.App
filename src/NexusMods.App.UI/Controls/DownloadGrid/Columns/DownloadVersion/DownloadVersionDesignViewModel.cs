using System.Reactive.Disposables;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Pages.Downloads.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadVersion;

public class DownloadVersionDesignViewModel : AViewModel<IDownloadVersionViewModel>, IDownloadVersionViewModel, IComparableColumn<IDownloadTaskViewModel>
{
    [Reactive]
    public IDownloadTaskViewModel Row { get; set; } = new DownloadTaskDesignViewModel();

    [Reactive]
    public string Version { get; set; } = "";

    public DownloadVersionDesignViewModel()
    {
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row.Version)
                .BindToUi(this, vm => vm.Version)
                .DisposeWith(d);
        });
    }
    
    // TODO: We should parse this as something like a NuGet (semantic) version and sort by that instead of a string comparison.
    public int Compare(IDownloadTaskViewModel a, IDownloadTaskViewModel b) => String.Compare(a.Version, b.Version, StringComparison.Ordinal);
}
