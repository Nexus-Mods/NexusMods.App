using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu;

public partial class LeftMenuView : ReactiveUserControl<ILeftMenuViewModel>
{
    public LeftMenuView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(x => x.ViewModel!.Items)
                .BindTo(this, x => x.MenuItemsControl.Items)
                .DisposeWith(d);
        });
    }
}

