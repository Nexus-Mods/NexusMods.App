using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

public partial class PreviewView : ReactiveUserControl<IPreviewViewModel>
{
    public PreviewView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Locations, view => view.LocationsPreviewsItemsControl.ItemsSource).DisposeWith(disposables);
        });
    }
}
