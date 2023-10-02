using Avalonia.ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public partial class PanelView : ReactiveUserControl<IPanelViewModel>
{
    public PanelView()
    {
        InitializeComponent();

        // this.WhenActivated(disposables =>
        // {
        //     this.OneWayBind(ViewModel, vm => vm.Content, view => view.ViewModelViewHost.ViewModel)
        //         .DisposeWith(disposables);
        // });
    }
}

