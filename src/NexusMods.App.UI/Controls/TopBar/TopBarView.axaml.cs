using System.Reactive.Disposables;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using NexusMods.App.UI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.TopBar;

public partial class TopBarView : ReactiveUserControl<ITopBarViewModel>
{
    public TopBarView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.BindCommand(ViewModel, vm => vm.LoginCommand, view => view.LoginButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.LogoutCommand, view => view.UserButton)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.IsLoggedIn, view => view.LoginButton.IsVisible,isLoggedIn => !isLoggedIn)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.IsLoggedIn, view => view.UserPanel.IsVisible)
                .DisposeWith(d);

            this.WhenAnyValue(
                    x => x.ViewModel!.IsLoggedIn,
                    x => x.ViewModel!.IsPremium,
                    (isLoggedIn, isPremium) => isLoggedIn && isPremium
                )
                .BindTo(this, view => view.Premium.IsVisible)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.Avatar, view => view.AvatarImage.Source)
                .DisposeWith(d);
        });
    }

}
