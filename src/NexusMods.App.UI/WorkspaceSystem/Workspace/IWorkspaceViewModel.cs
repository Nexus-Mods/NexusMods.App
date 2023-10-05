using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IWorkspaceViewModel : IViewModelInterface
{
    public ReadOnlyObservableCollection<IPanelViewModel> Panels { get; }
    public IReadOnlyList<IReadOnlyDictionary<PanelId, Rect>> PossibleStates { get; }

    /// <summary>
    /// Command for adding a new panel to the workspace.
    /// </summary>
    /// <remarks>
    /// This command returns the new panel.
    /// </remarks>
    public ReactiveCommand<IReadOnlyDictionary<PanelId, Rect>, IPanelViewModel> AddPanelCommand { get; }

    /// <summary>
    /// Command for remove an existing panel from the workspace.
    /// </summary>
    public ReactiveCommand<RemovePanelInput, Unit> RemovePanelCommand { get; }

    public void ArrangePanels(Size workspaceSize);
}
