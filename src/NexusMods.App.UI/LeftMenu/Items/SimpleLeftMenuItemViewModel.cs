using System.Reactive;
using NexusMods.Abstractions.UI;
using NexusMods.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class SimpleLeftMenuItemViewModel : AViewModel<INewLeftMenuItemViewModel>, INewLeftMenuItemViewModel
{
    [Reactive] public string Text { get; }
    [Reactive] public IconValue Icon { get; set; }
    [Reactive] public ReactiveCommand<Unit, Unit> NavigateCommand { get; }
    
    [Reactive] public bool IsActive { get; }
    
    [Reactive] public bool IsSelected { get; }

    SimpleLeftMenuItemViewModel()
    {
        Text = "";
        Icon = IconValues.LibraryOutline;
        NavigateCommand = ReactiveCommand.Create(() => { });
        IsActive = false;
        IsSelected = false;
    }
}
