using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IWorkspaceViewModel : IViewModelInterface
{
    public ReadOnlyObservableCollection<IPanelViewModel> Panels { get; }

    public ReactiveCommand<Unit, Unit> AddPanelCommand { get; }

    public ReactiveCommand<Unit, Unit> RemovePanelCommand { get; }
}
