using System.Reactive;
using NexusMods.Abstractions.UI;
using NexusMods.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public interface INewLeftMenuItemViewModel : IViewModelInterface, 
    // TODO: Remove this this after all old left menu items are replaced, used to allow using either in the meantime
    ILeftMenuItemViewModel
{
    public string Text { get; set; }
    
    public IconValue Icon { get; set; }
    
    public ReactiveCommand<Unit, Unit> NavigateCommand { get; }
    
    public bool IsActive { get; }
    
    public bool IsSelected { get; }
    
}
