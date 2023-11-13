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
            this.OneWayBind(ViewModel, vm => vm.TreeContainers, view => view.LocationsPreviewsItemsControl.ItemsSource)
                .DisposeWith(disposables);
        });
    }
}
