using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;
using R3;

namespace NexusMods.App.UI.Pages.DebugControls;

public partial class DebugControlsPageView : ReactiveUserControl<IDebugControlsPageViewModel>
{
    public DebugControlsPageView()
    {
        InitializeComponent();
        
        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.ShowAlphaViewCommand, v => v.OverlayOpenAlphaView.Command)
                .DisposeWith(disposables);
            
            this.OneWayBind(ViewModel, vm => vm.ShowMessageBoxOKCommand, v => v.OverlayShowMessageBoxOK.Command)
                .DisposeWith(disposables);
        });
    }
}

