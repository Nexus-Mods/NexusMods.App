using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public partial class WorkspacePlaygroundView : ReactiveUserControl<WorkspacePlaygroundViewModel>
{
    public WorkspacePlaygroundView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            ViewModelViewHost.ViewModel = ViewModel?.WorkspaceViewModel;

            this.BindCommand(ViewModel, vm => vm.WorkspaceViewModel.AddPanelCommand, view => view.AddPanelButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.WorkspaceViewModel.RemovePanelCommand, view => view.RemovePanelButton)
                .DisposeWith(disposables);
        });
    }
}

