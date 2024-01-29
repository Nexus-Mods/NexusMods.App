using System.Collections.ObjectModel;
using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IWorkspaceViewModel : IViewModelInterface
{
    public WorkspaceId Id { get; }

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
