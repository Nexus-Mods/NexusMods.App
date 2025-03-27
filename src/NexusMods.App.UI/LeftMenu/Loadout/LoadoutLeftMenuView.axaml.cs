using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.UI.Resources;
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
            
            this.OneWayBind(ViewModel, vm => vm.LeftMenuItemExternalChanges, view => view.ExternalChangesItem.ViewModel)
                .DisposeWith(disposables);
            
            this.WhenAnyValue(view => view.ViewModel!.LeftMenuItemExternalChanges)
                .Select(item => item != null)
                .BindTo(this, view => view.ExternalChangesItem.IsVisible)
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel!.LeftMenuCollectionItems)
                .BindTo(this, x => x.MenuItemsControl.ItemsSource)
                .DisposeWith(disposables);
            
            // Only show Loadout entry if number of collections is greater than 1
            this.WhenAnyValue(view => view.ViewModel!.LeftMenuCollectionItems.Count)
                .Select(count => count > 1)
                .BindTo(this, view => view.LoadoutItem.IsVisible)
                .DisposeWith(disposables);
            
            InstalledModsSectionText.Text = Language.LeftMenu_Label_Installed_Mods.ToUpperInvariant();
            UtilitiesSectionText.Text = Language.LeftMenu_Label_Utilities.ToUpperInvariant();
        });
    }
}
