using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class IconViewModel : AViewModel<IIconViewModel>, IIconViewModel
{
    [Reactive] public string Name { get; set; } = "";

    [Reactive] public IconValue Icon { get; set; } = new();

    [Reactive] public ReactiveCommand<NavigationInput, Unit> NavigateCommand { get; set; } =
        ReactiveCommand.Create<NavigationInput>(_ => { }, Observable.Return(false));
}
