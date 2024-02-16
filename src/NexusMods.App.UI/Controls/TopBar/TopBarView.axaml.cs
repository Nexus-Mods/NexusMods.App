using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
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

            this.BindCommand(ViewModel, vm => vm.HistoryActionCommand, view => view.HistoryActionButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.UndoActionCommand, view => view.UndoActionButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.RedoActionCommand, view => view.RedoActionButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.HelpActionCommand, view => view.HelpActionButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.SettingsActionCommand, view => view.SettingsActionButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.LogoutCommand, view => view.UserButton)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.IsLoggedIn, view => view.LoginButton.IsVisible,isLoggedIn => !isLoggedIn)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.IsLoggedIn, view => view.UserPanel.IsVisible)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.AddPanelDropDownViewModel, view => view.AddPanelViewModelViewHost.ViewModel)
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

            this.OneWayBind(ViewModel, vm => vm.ActiveWorkspaceTitle, view => view.ActiveWorkspaceTitleTextBlock.Text)
                .DisposeWith(d);
        });
    }

}
