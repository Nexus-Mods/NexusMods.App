using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu;

[UsedImplicitly]
public partial class LeftMenuView : ReactiveUserControl<ILeftMenuViewModel>
{
    public LeftMenuView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.Items, view => view.MenuItemsControl.Items)
                .DisposeWith(d);
        });
    }
}

