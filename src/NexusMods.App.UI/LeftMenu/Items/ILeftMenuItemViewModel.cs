using System.Reactive;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public interface ILeftMenuItemViewModel : IViewModelInterface 
{
    public string Text { get; set; }
    
    public IconValue Icon { get; set; }
    
    public ReactiveCommand<NavigationInformation, Unit> NavigateCommand { get; }
    
    public bool IsActive { get; }
    
    public bool IsSelected { get; }
    
    // ToggleSwitch related properties
    public bool IsToggleVisible { get; }
    
    public bool IsEnabled { get; set; }
    
    public ReactiveCommand<Unit, Unit> ToggleIsEnabledCommand { get; }
}
