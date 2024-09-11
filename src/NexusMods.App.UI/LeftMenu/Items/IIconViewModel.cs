using System.Reactive;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public interface IIconViewModel : ILeftMenuItemViewModel
{
    public string Name { get; set; }
    public IconValue Icon { get; set; }
    public string[] Badges { get; set; }
    public ReactiveCommand<NavigationInformation, Unit> NavigateCommand { get; set; }
    public int RelativeOrder { get; set; }
}
