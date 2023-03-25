using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Windows;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.TopBar, v => v.TopBar.ViewModel)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Spine, v => v.Spine.ViewModel)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.RightContent, v => v.RightContent.ViewModel)
                .DisposeWith(disposables);

            ViewModel.WhenAnyValue(v => v.RightContent)
                .Subscribe(_ => { })
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.TopBar.CloseCommand.IsExecuting)
                .SelectMany(e => e)
                .Where(e => e)
                .Subscribe(_ => Close())
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.TopBar.MinimizeCommand.IsExecuting)
                .SelectMany(e => e)
                .Where(e => e)
                .Subscribe(_ => WindowState = WindowState.Minimized)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.TopBar.ToggleMaximizeCommand.IsExecuting)
                .SelectMany(e => e)
                .Where(e => e)
                .Subscribe(_ => WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.LeftMenu)
                .BindTo(this, view => view.LeftMenuViewModelHost.ViewModel)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.OverlayContent)
                .BindTo(this, view => view.OverlayViewHost.ViewModel)
                .DisposeWith(disposables);
            this.WhenAnyValue(view => view.ViewModel!.OverlayContent)
                .Select(content => content != null)
                .BindTo(this, view => view.OverlayBorder.IsVisible)
                .DisposeWith(disposables);

        });
    }

    // ReSharper disable once UnusedParameter.Local
    /* TODO - find out why this is broken in Avalonia 0.11 - preview 5
    private void PointerPressed_Handler(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
    */
}
