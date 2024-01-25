using JetBrains.Annotations;

namespace NexusMods.App.UI.WorkspaceSystem;

[PublicAPI]
public interface IWorkspaceController
{
    /// <summary>
    /// Swaps the positions of two panels.
    /// </summary>
    public void SwapPanels(PanelId firstPanelId, PanelId secondPanelId);

    /// <summary>
    /// Closes the panel with the given ID.
    /// </summary>
    public void ClosePanel(PanelId panelToClose);

    /// <summary>
    /// Transforms the current state of the workspace into a serializable data format.
    /// </summary>
    public WorkspaceData ToData();

    /// <summary>
    /// Applies <paramref name="data"/> to the workspace.
    /// </summary>
    public void FromData(WorkspaceData data);
}
