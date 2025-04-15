using System.Reactive.Disposables;
using Avalonia.Interactivity;
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
            this.OneWayBind(ViewModel, vm => vm.GenerateUnhandledException, v => v.GenerateUnhandledException.Command)
                .DisposeWith(disposables);
            
            this.OneWayBind(ViewModel, vm => vm.MarkdownRenderer, v => v.MarkdownRendererViewModelViewHost.ViewModel)
                .DisposeWith(disposables);
            
            this.OneWayBind(ViewModel, vm => vm.ShowModalOK, v => v.ShowModalOK.Command)
                .DisposeWith(disposables);
            
            this.OneWayBind(ViewModel, vm => vm.ShowModalOKCancel, v => v.ShowModalOKCancel.Command)
                .DisposeWith(disposables);
            
            this.OneWayBind(ViewModel, vm => vm.ShowModeless, v => v.ShowModeless.Command)
                .DisposeWith(disposables);
        });
        
        
    }
}

