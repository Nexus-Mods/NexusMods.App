using System.Windows.Input;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Controls.Spine.Buttons;

public interface ISpineItemViewModel : IViewModelInterface
{
    /// <summary>
    /// Is the spine item active and highlighted.
    /// </summary>
    bool IsActive { get; set; }
    
    /// <summary>
    /// Command to execute when the spine item is clicked.
    /// </summary>
    ICommand Click { get; set; }
    
    /// <summary>
    /// The context of the workspace associated with the spine item.
    /// </summary>
    IWorkspaceContext? WorkspaceContext { get; set; }
    
}
