using System.Collections.ObjectModel;

namespace NexusMods.App.UI.LeftMenu;

public interface ILeftMenuViewModel : IViewModelInterface
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }
}
