using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.UI.Sdk;

namespace NexusMods.App.UI.LeftMenu;

public interface ILeftMenuViewModel : IViewModelInterface
{
    /// <summary>
    /// The Id of the workspace this left menu is attached to.
    /// </summary>
    public WorkspaceId WorkspaceId { get; }
}
