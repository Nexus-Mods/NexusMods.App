using System.Collections.ObjectModel;
using NexusMods.App.UI.RightContent;

namespace NexusMods.App.UI.LeftMenu;

public interface ILeftMenuViewModel : IViewModelInterface
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }
}
