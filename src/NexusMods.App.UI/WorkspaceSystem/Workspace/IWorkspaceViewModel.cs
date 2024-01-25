using System.Collections.ObjectModel;
using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IWorkspaceViewModel : IViewModelInterface, IWorkspaceController
{
    public ReadOnlyObservableCollection<IPanelViewModel> Panels { get; }

    public ReadOnlyObservableCollection<IPanelResizerViewModel> Resizers { get; }

    public ReadOnlyObservableCollection<IAddPanelButtonViewModel> AddPanelButtonViewModels { get; }

    public bool IsHorizontal { get; }

    /// <summary>
    /// Called by the View to notify the VM about the new size of the control.
    /// </summary>
    public void Arrange(Size workspaceSize);
}
