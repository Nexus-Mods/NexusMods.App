using System.Reactive.Disposables;
using System.Reactive.Linq;
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
            this.BindCommand(ViewModel, vm => vm.LoginCommand, v => v.LoginButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.LogoutCommand, view => view.UserButton)
                .DisposeWith(d);

            this.WhenAnyValue(v => v.ViewModel!.IsLoggedIn)
                .Select(v => !v)
                .BindToUi(this, v => v.LoginButton.IsVisible)
                .DisposeWith(d);

            this.WhenAnyValue(v => v.ViewModel!.IsLoggedIn)
                .Select(v => v)
                .BindToUi(this, v => v.UserPanel.IsVisible)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.IsLoggedIn)
                .CombineLatest(this.WhenAnyValue(view => view.ViewModel!.IsPremium))
                .Select(t => t.First && t.Second)
                .BindToUi(this, view => view.Premium.IsVisible)
                .DisposeWith(d);

            ViewModel.WhenAnyValue(vm => vm.Avatar)
                .WhereNotNull()
                .BindToUi(AvatarImage, v => v.Source)
                .DisposeWith(d);
            
        });
    }

}
