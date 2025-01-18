using NexusMods.Abstractions.UI;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.LeftMenu;

public interface ILeftMenuViewModel : IViewModelInterface
{
    /// <summary>
    /// The Id of the workspace this left menu is attached to.
    /// </summary>
    public WorkspaceId WorkspaceId { get; }
}
