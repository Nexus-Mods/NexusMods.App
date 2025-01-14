using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu;

[UsedImplicitly]
public partial class EmptyLeftMenuView : ReactiveUserControl<IEmptyLeftMenuViewModel>
{
    public EmptyLeftMenuView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.LeftMenuItemEmpty, view => view.EmptyLeftMenuItem.ViewModel)
                .DisposeWith(d);
        });
    }
}

