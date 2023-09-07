using System.Windows.Input;
using NexusMods.App.UI.Icons;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class IconViewModel : AViewModel<IIconViewModel>, IIconViewModel
{
    [Reactive] public string Name { get; set; } = "";

    [Reactive]
    public IconType Icon { get; set; }

    [Reactive] public ICommand Activate { get; set; } = Initializers.ICommand;
}
