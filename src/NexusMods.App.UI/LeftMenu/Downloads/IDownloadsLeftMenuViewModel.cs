using System.Collections.ObjectModel;
using NexusMods.App.UI.LeftMenu.Items;

namespace NexusMods.App.UI.LeftMenu.Downloads;

public interface IDownloadsLeftMenuViewModel : ILeftMenuViewModel
{
    public ILeftMenuItemViewModel LeftMenuItemAllDownloads { get; }
    
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> LeftMenuItemsPerGameDownloads { get; }
}