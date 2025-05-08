using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

[ExcludeFromCodeCoverage]
public partial class PreviewView : ReactiveUserControl<IPreviewViewModel>
{
    public PreviewView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // Bind the tree to the data source.
            this.OneWayBind(ViewModel, vm => vm.Tree,
                    view => view.LocationPreviewTreeDataGrid.Source!)
                .DisposeWith(disposables);

            // A hack around https://github.com/AvaloniaUI/Avalonia.Controls.TreeDataGrid/issues/221
            LocationPreviewTreeDataGrid.Width = double.NaN;
        });
    }
}
