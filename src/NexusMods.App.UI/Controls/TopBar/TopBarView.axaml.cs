using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;
using ReactiveUI;
using Image = Avalonia.Controls.Image;

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

            var avatar = UserButton.FindDescendantOfType<Image>();
            ViewModel.WhenAnyValue(vm => vm.Avatar)
                .Select(e => e)
                .BindToUi(avatar, v => v.Source)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.CloseCommand, v => v.WindowCloseButton)
                .DisposeWith(d);
            this.BindCommand(ViewModel, vm => vm.MinimizeCommand, v => v.WindowMinimizeButton)
                .DisposeWith(d);
            this.BindCommand(ViewModel, vm => vm.MaximizeCommand, v => v.WindowMaximizeButton)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.ShowWindowControls)
                .Subscribe(SetShowWindowControls)
                .DisposeWith(d);
        });
    }

    public void SetShowWindowControls(bool shouldShow)
    {
        if (shouldShow)
        {
            MainGrid.ColumnDefinitions.Last().Width = GridLength.Auto;
            WindowControlsBorder.IsVisible = true;
        }
        else
        {
            MainGrid.ColumnDefinitions.Last().Width = new GridLength(0);
            WindowControlsBorder.IsVisible = false;
        }
    }
}
