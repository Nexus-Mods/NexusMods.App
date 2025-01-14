using System.Collections.ObjectModel;
using NexusMods.App.UI.LeftMenu.Items;

namespace NexusMods.App.UI.LeftMenu.Loadout;

public interface ILoadoutLeftMenuViewModel : ILeftMenuViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }
    
    public IApplyControlViewModel ApplyControlViewModel { get; }
    
    public INewLeftMenuItemViewModel LeftMenuItemLibrary { get; }
    
    public INewLeftMenuItemViewModel LeftMenuItemLoadout { get; }
    
    public INewLeftMenuItemViewModel LeftMenuItemHealthCheck { get; }
}
