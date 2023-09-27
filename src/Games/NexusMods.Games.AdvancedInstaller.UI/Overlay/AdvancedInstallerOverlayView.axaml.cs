using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public partial class AdvancedInstallerOverlayView : ReactiveUserControl<IAdvancedInstallerOverlayViewModel>
{
    public AdvancedInstallerOverlayView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {

            this.OneWayBind(ViewModel, vm => vm.BodyViewModel, v => v.TopContentViewHost.ViewModel)
                .DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.FooterViewModel, v => v.BottomContentViewHost.ViewModel)
                .DisposeWith(disposables);
        });
    }
}

