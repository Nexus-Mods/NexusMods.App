using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.MyLoadouts;

public partial class MyLoadoutsView : ReactiveUserControl<IMyLoadoutsViewModel>
{
    public MyLoadoutsView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.GameSectionVMs, v => v.GameSectionsItemsControl.ItemsSource)
                .DisposeWith(d);
            
        });
    }
}

