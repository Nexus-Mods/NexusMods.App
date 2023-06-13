using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Home;

public partial class HomeLeftMenuView : ReactiveUserControl<IHomeLeftMenuViewModel>
{
    public HomeLeftMenuView()
    {
        InitializeComponent();


        this.WhenActivated(d =>
        {
            this.WhenAnyValue(x => x.ViewModel!.Items)
                .BindTo(this, x => x.MenuItemsControl.ItemsSource)
                .DisposeWith(d);
        });
    }
}

