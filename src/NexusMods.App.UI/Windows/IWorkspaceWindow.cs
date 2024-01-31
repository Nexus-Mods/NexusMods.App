using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Windows;

public interface IWorkspaceWindow
{
    public WindowId WindowId { get; }

    public bool IsActive { get; }

    public IWorkspaceViewModel Workspace { get; set; }
}
