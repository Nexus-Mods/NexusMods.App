using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Game;

public partial class GameLeftMenuView : ReactiveUserControl<IGameLeftMenuViewModel>
{
    public GameLeftMenuView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(x => x.ViewModel!.LaunchButton)
                .BindTo(this, x => x.LaunchButton.ViewModel)
                .DisposeWith(d);

            this.WhenAnyValue(x => x.ViewModel!.Items)
                .BindTo(this, x => x.MenuItemsControl.Items)
                .DisposeWith(d);
        });
    }
}

