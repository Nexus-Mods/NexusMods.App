using System.Collections.ObjectModel;
using NexusMods.App.UI.WorkspaceSystem;


namespace NexusMods.App.UI.LeftMenu;

public class EmptyLeftMenuViewModel(WorkspaceId workspaceId) : AViewModel<ILeftMenuViewModel>, ILeftMenuViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; } = new([]);
    public WorkspaceId WorkspaceId { get; } = workspaceId;
}
