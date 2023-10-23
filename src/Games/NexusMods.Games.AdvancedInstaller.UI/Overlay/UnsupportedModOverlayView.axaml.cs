using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public partial class UnsupportedModOverlayView : ReactiveUserControl<IUnsupportedModOverlayViewModel>
{
    public UnsupportedModOverlayView()
    {
        this.WhenActivated(d =>
        {
            this.BindCommand(ViewModel, vm => vm.AcceptCommand, view => view.InstallButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.DeclineCommand, view => view.CancelButton)
                .DisposeWith(d);
        });
        InitializeComponent();
    }
}
