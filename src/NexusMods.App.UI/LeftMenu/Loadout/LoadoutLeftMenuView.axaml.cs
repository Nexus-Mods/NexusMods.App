using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Loadout;

[UsedImplicitly]
public partial class LoadoutLeftMenuView : ReactiveUserControl<ILoadoutLeftMenuViewModel>
{
    public LoadoutLeftMenuView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.ApplyControlViewModel, view => view.ApplyControlViewHost.ViewModel)
                .DisposeWith(disposables);
            
            this.OneWayBind(ViewModel, vm => vm.LeftMenuItemLibrary, view => view.LibraryItem.ViewModel)
                .DisposeWith(disposables);
            
            this.OneWayBind(ViewModel, vm => vm.LeftMenuItemLoadout, view => view.LoadoutItem.ViewModel)
                .DisposeWith(disposables);
            
            this.OneWayBind(ViewModel, vm => vm.LeftMenuItemHealthCheck, view => view.HealthCheckItem.ViewModel)
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel!.LeftMenuCollectionItems)
                .BindTo(this, x => x.MenuItemsControl.ItemsSource)
                .DisposeWith(disposables);
        });
    }
}
