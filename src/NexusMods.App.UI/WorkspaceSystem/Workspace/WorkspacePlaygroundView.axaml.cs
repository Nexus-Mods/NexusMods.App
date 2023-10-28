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

            this.OneWayBind(ViewModel, vm => vm.WorkspaceViewModel.AddPanelButtonViewModels, view => view.AddPanelButtonItemsControl.ItemsSource)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.SaveWorkspaceCommand, view => view.SaveWorkspaceButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.LoadWorkspaceCommand, view => view.LoadWorkspaceButton)
                .DisposeWith(disposables);
        });
    }
}

