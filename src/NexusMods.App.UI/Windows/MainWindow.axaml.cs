using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Windows;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

        #if DEBUG
        this.AttachDevTools();
        #endif

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.TopBar, v => v.TopBar.ViewModel)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Spine, v => v.Spine.ViewModel)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.DevelopmentBuildBanner, v => v.DevelopmentBuildBanner.ViewModel)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.WorkspaceController.AllWorkspaces, view => view.WorkspacesItemsControl.ItemsSource)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.LeftMenu)
                .BindTo(this, view => view.LeftMenuViewModelHost.ViewModel)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.CurrentOverlay)
                .BindTo(this, view => view.OverlayViewHost.ViewModel)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.CurrentOverlay)
                .Select(content => content != null)
                .BindTo(this, view => view.OverlayBorder.IsVisible)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.IsActive)
                .BindTo(this, view => view.ViewModel!.IsActive)
                .DisposeWith(disposables);

            this.WhenAnyObservable(view => view.ViewModel!.BringWindowToFront)
                .OnUI()
                .Where(static shouldBringToFront => shouldBringToFront)
                .Subscribe(_ => {
                    if (WindowState == WindowState.Minimized)
                        WindowState = WindowState.Normal;

                    Activate();
                }).DisposeWith(disposables);
        });
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        ViewModel?.OnClose();
        base.OnClosing(e);
    }
}
