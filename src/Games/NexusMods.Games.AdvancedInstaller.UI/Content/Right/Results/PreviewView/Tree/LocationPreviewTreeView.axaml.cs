using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

[ExcludeFromCodeCoverage]
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
