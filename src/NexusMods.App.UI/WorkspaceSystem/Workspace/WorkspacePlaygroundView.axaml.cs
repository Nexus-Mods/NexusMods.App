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

            this.BindCommand(ViewModel, vm => vm.RemovePanelCommand, view => view.RemovePanelButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.SwapPanelsCommand, view => view.SwapPanelsButton)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.WorkspaceViewModel.AddPanelButtonViewModels, view => view.AddPanelButtonItemsControl.ItemsSource)
                .DisposeWith(disposables);
        });
    }
}

