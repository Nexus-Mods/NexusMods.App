using System.Collections.ObjectModel;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.LeftMenu.Loadout;

public class LoadoutLeftMenuDesignViewModel : AViewModel<ILoadoutLeftMenuViewModel>, ILoadoutLeftMenuViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; } = new([]);
    public WorkspaceId WorkspaceId { get; } = new();
    public ILaunchButtonViewModel LaunchButtonViewModel { get; } = new LaunchButtonDesignViewModel();
}
