using System.Reactive.Disposables;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Pages.Downloads.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadName;

public class DownloadNameDesignViewModel : AViewModel<IDownloadNameViewModel>, IDownloadNameViewModel, IComparableColumn<IDownloadTaskViewModel>
{
    [Reactive]
    public IDownloadTaskViewModel Row { get; set; } = new DownloadTaskDesignViewModel();

    [Reactive]
    public string Name { get; set; } = "";

    public DownloadNameDesignViewModel()
    {
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row.Name)
                .BindToUi(this, vm => vm.Name)
                .DisposeWith(d);
        });
    }
    
    public int Compare(IDownloadTaskViewModel a, IDownloadTaskViewModel b) => String.Compare(a.Name, b.Name, StringComparison.Ordinal);
}
