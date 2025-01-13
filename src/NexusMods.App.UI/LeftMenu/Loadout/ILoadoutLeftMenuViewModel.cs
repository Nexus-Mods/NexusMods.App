using NexusMods.App.UI.LeftMenu.Items;

namespace NexusMods.App.UI.LeftMenu.Loadout;

public interface ILoadoutLeftMenuViewModel : ILeftMenuViewModel
{
    public IApplyControlViewModel ApplyControlViewModel { get; }
    
    public INewLeftMenuItemViewModel LeftMenuItemLibrary { get; }
    
    public INewLeftMenuItemViewModel LeftMenuItemLoadout { get; }
    
    public INewLeftMenuItemViewModel LeftMenuItemHealthCheck { get; }
}
