using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using NexusMods.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.TopBar;

public partial class TopBarView : ReactiveUserControl<ITopBarViewModel>
{
    public TopBarView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.ActiveWorkspaceTitle, view => view.ActiveWorkspaceTitleTextBlock.Text)
                .DisposeWith(d);


            this.OneWayBind(ViewModel, vm => vm.AddPanelDropDownViewModel, view => view.AddPanelViewModelViewHost.ViewModel)
                .DisposeWith(d);


            this.BindCommand(ViewModel, vm => vm.OpenSettingsCommand, view => view.OpenSettingsMenuItem)
                .DisposeWith(d);


            this.BindCommand(ViewModel, vm => vm.ViewChangelogCommand, view => view.ViewChangelogMenuItem)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.ViewAppLogsCommand, view => view.ViewAppLogsMenuItem)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.GiveFeedbackCommand, view => view.GiveFeedbackMenuItem)
                .DisposeWith(d);


            this.BindCommand(ViewModel, vm => vm.LoginCommand, view => view.LoginButton)
                .DisposeWith(d);


            this.OneWayBind(ViewModel, vm => vm.Avatar, view => view.AvatarIcon.Value, avatar => new IconValue(new AvaloniaImage(avatar)))
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.OpenNexusModsProfileCommand, view => view.OpenNexusModsProfileMenuItem)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.OpenNexusModsAccountSettingsCommand, view => view.OpenNexusModsAccountSettingsMenuItem)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.LogoutCommand, view => view.SignOutMenuItem)
                .DisposeWith(d);
            
            this.WhenAnyValue(
                    x => x.ViewModel!.IsLoggedIn,
                    x => x.ViewModel!.IsPremium,
                    (isLoggedIn, isPremium) => isLoggedIn && isPremium
                )
                .BindToView (this, view => view.PremiumLabel.IsVisible)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.IsLoggedIn, view => view.LoginMenuItem.IsVisible, b => !b)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.IsLoggedIn, view => view.AvatarMenuItem.IsVisible)
                .DisposeWith(d);
        });
    }

}
