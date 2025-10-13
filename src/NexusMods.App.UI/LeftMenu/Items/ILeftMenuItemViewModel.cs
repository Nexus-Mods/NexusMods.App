using System.Reactive;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public interface ILeftMenuItemViewModel : IViewModelInterface 
{
    public StringComponent Text { get; }
    
    public IconValue Icon { get; set; }
    
    public string ToolTipText { get; }
    
    public ReactiveCommand<NavigationInformation, Unit> NavigateCommand { get; }
    
    public bool IsActive { get; }
    
    public bool IsSelected { get; }
}
