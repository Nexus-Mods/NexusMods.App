using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI;

[ExcludeFromCodeCoverage]
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

            this.BindCommand(ViewModel, vm => vm.DeclineCommand, view => view.OverlayHeaderCloseButton)
                .DisposeWith(d);
        });
        InitializeComponent();
    }
}
