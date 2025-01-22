using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Home;

[UsedImplicitly]
public partial class HomeLeftMenuView : ReactiveUserControl<IHomeLeftMenuViewModel>
{
    public HomeLeftMenuView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.LeftMenuItemMyGames, view => view.MyGamesItem.ViewModel)
                .DisposeWith(d);
            
            this.OneWayBind(ViewModel, vm => vm.LeftMenuItemMyLoadouts, view => view.MyLoadoutsItem.ViewModel)
                .DisposeWith(d);
        });
    }
}

