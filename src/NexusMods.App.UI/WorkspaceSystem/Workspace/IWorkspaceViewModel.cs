using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media.Imaging;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IWorkspaceViewModel : IViewModelInterface
{
    public ReadOnlyObservableCollection<IPanelViewModel> Panels { get; }
    public IReadOnlyList<IReadOnlyDictionary<PanelId, Rect>> PossibleStates { get; }
    public IReadOnlyList<Bitmap> StateImages { get; }

    /// <summary>
    /// Add a new panel to the workspace.
    /// </summary>
    /// <param name="state"></param>
    /// <returns>The newly created <see cref="IPanelViewModel"/>.</returns>
    public IPanelViewModel AddPanel(IReadOnlyDictionary<PanelId, Rect> state);

    /// <summary>
    /// Gets whether <see cref="AddPanel"/> can be called.
    /// </summary>
    public bool CanAddPanel { get; }

    /// <summary>
    /// Remove an existing panel from the workspace.
    /// </summary>
    public void RemovePanel(RemovePanelInput removePanelInput);

    /// <summary>
    /// Gets whether <see cref="RemovePanel"/> can be called.
    /// </summary>
    public bool CanRemovePanel { get; }

    /// <summary>
    /// Called by the View to notify the VM about the new size of the control.
    /// </summary>
    public void ArrangePanels(Size workspaceSize);
}
