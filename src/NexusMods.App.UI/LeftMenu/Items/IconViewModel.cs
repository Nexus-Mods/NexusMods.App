using System.Windows.Input;
using NexusMods.Icons;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class IconViewModel : AViewModel<IIconViewModel>, IIconViewModel
{
    [Reactive] public string Name { get; set; } = "";

    [Reactive] public IconValue Icon { get; set; } = new();

    [Reactive] public ICommand Activate { get; set; } = Initializers.ICommand;
}
