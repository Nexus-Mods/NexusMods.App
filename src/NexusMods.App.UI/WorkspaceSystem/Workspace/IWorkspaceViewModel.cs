using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IWorkspaceViewModel : IViewModelInterface
{
    public ReadOnlyObservableCollection<IPanelViewModel> Panels { get; }

    public ReactiveCommand<AddPanelInput, IPanelViewModel> AddPanelCommand { get; }

    public ReactiveCommand<RemovePanelInput, Unit> RemovePanelCommand { get; }

    public void ArrangePanels(Size workspaceControlSize);
}
