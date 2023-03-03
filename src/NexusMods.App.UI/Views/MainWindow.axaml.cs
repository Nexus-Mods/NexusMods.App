using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.ViewModels;
using ReactiveUI;

namespace NexusMods.App.UI.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Spine, v => v.Spine.ViewModel)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.RightContent, v => v.RightContent.ViewModel)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.TopBarViewModel, v => v.TopBar.ViewModel)
                .DisposeWith(disposables);

            ViewModel.WhenAnyValue(v => v.RightContent)
                .Subscribe(x => { })
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.TopBarViewModel.CloseCommand.IsExecuting)
                .SelectMany(e => e)
                .Where(e => e)
                .Subscribe(_ => Close())
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.TopBarViewModel.MinimizeCommand.IsExecuting)
                .SelectMany(e => e)
                .Where(e => e)
                .Subscribe(_ => WindowState = WindowState.Minimized)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.TopBarViewModel.MaximizeCommand.IsExecuting)
                .SelectMany(e => e)
                .Where(e => e)
                .Subscribe(_ => WindowState = WindowState.Maximized)
                .DisposeWith(disposables);

        });
    }

    private void PointerPressed_Handler(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
