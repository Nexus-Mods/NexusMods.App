using System.Windows.Input;
using NexusMods.Icons;

namespace NexusMods.App.UI.LeftMenu.Items;

public interface IIconViewModel : ILeftMenuItemViewModel
{
    public string Name { get; set; }
    public IconValue Icon { get; set; }

    public ICommand Activate { get; set; }
}
