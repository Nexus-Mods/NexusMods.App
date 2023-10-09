using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

[UsedImplicitly]
public partial class WorkspacePlaygroundView : ReactiveUserControl<WorkspacePlaygroundViewModel>
{
    public WorkspacePlaygroundView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            ViewModelViewHost.ViewModel = ViewModel?.WorkspaceViewModel;

            this.BindCommand(ViewModel, vm => vm.AddPanelCommand, view => view.AddPanelButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.RemovePanelCommand, view => view.RemovePanelButton)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.WorkspaceViewModel.StateImages, view => view.StateImages.ItemsSource)
                .DisposeWith(disposables);
        });
    }
}

