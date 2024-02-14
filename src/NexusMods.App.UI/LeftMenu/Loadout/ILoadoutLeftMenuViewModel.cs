using NexusMods.App.UI.LeftMenu.Items;

namespace NexusMods.App.UI.LeftMenu.Loadout;

public interface ILoadoutLeftMenuViewModel : ILeftMenuViewModel
{
    public ILaunchButtonViewModel LaunchButtonViewModel { get; }
}
