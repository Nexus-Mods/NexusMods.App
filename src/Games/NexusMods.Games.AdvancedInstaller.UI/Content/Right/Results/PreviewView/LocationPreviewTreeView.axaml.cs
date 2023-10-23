using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

public partial class LocationPreviewTreeView : ReactiveUserControl<ILocationPreviewTreeViewModel>
{
    public LocationPreviewTreeView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Tree, view => view.LocationPreviewTreeDataGrid.Source!)
                .DisposeWith(disposables);
        });
    }
}
