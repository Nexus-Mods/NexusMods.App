using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.ReactiveUI;
using NexusMods.App.UI;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

public partial class GuidedInstallerWindow : ReactiveWindow<IGuidedInstallerWindowViewModel>
{            
    public GuidedInstallerWindow()
    {
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = -1;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = true;
        
        InitializeComponent();

#if DEBUG
        this.AttachDevTools();
#endif

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.WindowName, view => view.Title)
                .DisposeWith(disposables);
            
            this.OneWayBind(ViewModel, vm => vm.WindowName, view => view.TitleTextBlock.Text, s => $"Install Options - {s}")
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.ActiveStepViewModel, view => view.StepViewHost.ViewModel)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.CloseCommand.IsExecuting)
                .SelectMany(e => e)
                .Where(e => e)
                .SubscribeWithErrorLogging(logger: default, _ => Close())
                .DisposeWith(disposables);
        });
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}

