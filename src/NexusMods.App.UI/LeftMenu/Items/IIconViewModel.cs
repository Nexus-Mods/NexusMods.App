using System.Reactive;
using System.Windows.Input;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public interface IIconViewModel : ILeftMenuItemViewModel
{
    public string Name { get; set; }
    public IconValue Icon { get; set; }
    public ReactiveCommand<NavigationInput, Unit> NavigateCommand { get; set; }
}
