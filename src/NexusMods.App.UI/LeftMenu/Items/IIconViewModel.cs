using System.Windows.Input;
using NexusMods.App.UI.Icons;

namespace NexusMods.App.UI.LeftMenu.Items;

public interface IIconViewModel : ILeftMenuItemViewModel
{
    public string Name { get; set; }
    public IconType Icon { get; set; }

    public ICommand Activate { get; set; }
}
