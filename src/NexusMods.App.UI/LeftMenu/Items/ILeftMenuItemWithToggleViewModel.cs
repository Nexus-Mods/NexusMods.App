using System.Reactive;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public interface ILeftMenuItemWithToggleViewModel : ILeftMenuItemViewModel
{
    public bool IsToggleVisible { get; }
    
    public bool IsEnabled { get; set; }
    
    public ReactiveCommand<Unit, Unit> ToggleIsEnabledCommand { get; }
}
