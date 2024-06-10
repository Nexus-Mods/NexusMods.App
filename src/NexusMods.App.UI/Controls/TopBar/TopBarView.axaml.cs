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
            this.BindCommand(ViewModel, vm => vm.LoginCommand, view => view.LoginButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.HelpActionCommand, view => view.HelpActionButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.OpenSettingsCommand, view => view.SettingsActionButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.LogoutCommand, view => view.SignOutMenuItem)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.OpenNexusModsProfileCommand, view => view.OpenNexusModsProfileMenuItem)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.OpenNexusModsAccountSettingsCommand, view => view.OpenNexusModsAccountSettingsMenuItem)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.AddPanelDropDownViewModel, view => view.AddPanelViewModelViewHost.ViewModel)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.Avatar, view => view.AvatarMenuItemHeader.Icon, avatar => new IconValue(new AvaloniaImage(avatar)))
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.ActiveWorkspaceTitle, view => view.ActiveWorkspaceTitleTextBlock.Text)
                .DisposeWith(d);
        });
    }

}
