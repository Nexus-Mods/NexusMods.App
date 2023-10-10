using System.Collections.ObjectModel;
using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IWorkspaceViewModel : IViewModelInterface
{
    public ReadOnlyObservableCollection<IPanelViewModel> Panels { get; }

    public IReadOnlyList<IAddPanelButtonViewModel> AddPanelButtonViewModels { get; }

    /// <summary>
    /// Called by the View to notify the VM about the new size of the control.
    /// </summary>
    public void ArrangePanels(Size workspaceSize);

    /// <summary>
    /// Swaps the position of two panels.
    /// </summary>
    public void SwapPanels(IPanelViewModel first, IPanelViewModel second);

    /// <summary>
    /// Add a new panel to the workspace.
    /// </summary>
    /// <returns>The newly created <see cref="IPanelViewModel"/>.</returns>
    public IPanelViewModel AddPanel(IReadOnlyDictionary<PanelId, Rect> state);

    /// <summary>
    /// Closes a panel from the workspace.
    /// </summary>
    public void ClosePanel(IPanelViewModel currentPanel);
}
