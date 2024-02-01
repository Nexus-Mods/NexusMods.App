using System.Collections.ObjectModel;
using Avalonia;
using NexusMods.App.UI.Windows;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IWorkspaceViewModel : IViewModelInterface
{
    /// <summary>
    /// Gets the ID of the workspace.
    /// </summary>
    public WorkspaceId Id { get; }

    /// <summary>
    /// Gets the ID of the window this workspace is in.
    /// </summary>
    public WindowId WindowId { get; }

    /// <summary>
    /// Gets or sets whether this is the currently visible workspace in the window.
    /// </summary>
    public bool IsActive { get; set; }

    public ReadOnlyObservableCollection<IPanelViewModel> Panels { get; }

    public ReadOnlyObservableCollection<IPanelResizerViewModel> Resizers { get; }

    public ReadOnlyObservableCollection<IAddPanelButtonViewModel> AddPanelButtonViewModels { get; }

    public bool IsHorizontal { get; }

    /// <summary>
    /// Called by the View to notify the VM about the new size of the control.
    /// </summary>
    public void Arrange(Size workspaceSize);

    /// <summary>
    /// Transforms the current state of the workspace into a serializable data format.
    /// </summary>
    public WorkspaceData ToData();

    /// <summary>
    /// Applies <paramref name="data"/> to the workspace.
    /// </summary>
    public void FromData(WorkspaceData data);
}
