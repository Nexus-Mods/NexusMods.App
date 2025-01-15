using NexusMods.App.UI.LeftMenu.Items;

namespace NexusMods.App.UI.LeftMenu;

public interface IEmptyLeftMenuViewModel : ILeftMenuViewModel
{
    public ILeftMenuItemViewModel LeftMenuItemEmpty { get; }
}
