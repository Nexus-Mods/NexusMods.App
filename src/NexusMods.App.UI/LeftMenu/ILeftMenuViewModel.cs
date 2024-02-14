using System.Collections.ObjectModel;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.LeftMenu;

public interface ILeftMenuViewModel : IViewModelInterface
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }

    /// <summary>
    /// The Id of the workspace this left menu is attached to.
    /// </summary>
    public WorkspaceId WorkspaceId { get; }
}
