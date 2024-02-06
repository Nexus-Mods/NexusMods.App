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
            this.WhenAnyValue(x => x.ViewModel!.Items)
                .BindTo(this, x => x.MenuItemsControl.ItemsSource)
                .DisposeWith(d);
        });
    }
}

