using System.Reactive;
using NexusMods.Abstractions.UI;
using NexusMods.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class SimpleLeftMenuItemViewModel : AViewModel<INewLeftMenuItemViewModel>, INewLeftMenuItemViewModel
{
    [Reactive] public string Text { get; set; }
    [Reactive] public IconValue Icon { get; set; }
    [Reactive] public ReactiveCommand<Unit, Unit> NavigateCommand { get; private set; }
    
    [Reactive] public bool IsActive { get; private set; }
    
    [Reactive] public bool IsSelected { get; private set; }

    public SimpleLeftMenuItemViewModel()
    {
        Text = "VM Text";
        Icon = IconValues.LibraryOutline;
        NavigateCommand = ReactiveCommand.Create(() => { });
        IsActive = false;
        IsSelected = false;
    }
}
