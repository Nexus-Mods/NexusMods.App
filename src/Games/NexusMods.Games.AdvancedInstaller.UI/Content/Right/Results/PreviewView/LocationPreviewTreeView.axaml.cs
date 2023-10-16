using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

public partial class LocationPreviewTreeView : ReactiveUserControl<ILocationPreviewTreeViewModel>
{
    public LocationPreviewTreeView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind<
                        ILocationPreviewTreeViewModel, LocationPreviewTreeView, HierarchicalTreeDataGridSource<ITreeEntryViewModel>,
                        ITreeDataGridSource>
                    (ViewModel, vm => vm.Tree, view => view.LocationPreviewTreeDataGrid.Source!)
                .DisposeWith(disposables);
        });
    }

}

