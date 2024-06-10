using System.Reactive;
using System.Reactive.Linq;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class IconViewModel : AViewModel<IIconViewModel>, IIconViewModel
{
    [Reactive] public string Name { get; set; } = "";

    [Reactive] public IconValue Icon { get; set; } = new();

    [Reactive] public string[] Badges { get; set; } = [];

    [Reactive] public ReactiveCommand<NavigationInformation, Unit> NavigateCommand { get; set; } = ReactiveCommand.Create<NavigationInformation>(_ => { }, Observable.Return(true));
}
