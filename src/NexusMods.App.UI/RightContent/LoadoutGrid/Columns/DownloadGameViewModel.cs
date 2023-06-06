using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public class DownloadGameViewModel : AViewModel<IDownloadGameViewModel>, IDownloadGameViewModel, IComparableColumn<IDownloadTaskViewModel>
{
    [Reactive]
    public IDownloadTaskViewModel Row { get; set; } = new DownloadTaskDesignViewModel();

    [Reactive]
    public string Game { get; set; } = "";

    public DownloadGameViewModel()
    {
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row.Game)
                .BindToUi(this, vm => vm.Game)
                .DisposeWith(d);
        });
    }

    public int Compare(IDownloadTaskViewModel a, IDownloadTaskViewModel b) => String.Compare(a.Game, b.Game, StringComparison.Ordinal);
}
