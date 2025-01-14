using NexusMods.App.UI.LeftMenu.Items;

namespace NexusMods.App.UI.LeftMenu.Home;

public interface IHomeLeftMenuViewModel : ILeftMenuViewModel
{
    public INewLeftMenuItemViewModel LeftMenuItemMyGames { get; }
    
    public INewLeftMenuItemViewModel LeftMenuItemMyLoadouts { get; }
    
}
