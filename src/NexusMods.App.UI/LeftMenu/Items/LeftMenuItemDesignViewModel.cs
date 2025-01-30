using System.Reactive;
using System.Reactive.Disposables;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class LeftMenuItemDesignViewModel : AViewModel<ILeftMenuItemViewModel>, ILeftMenuItemWithToggleViewModel
{
    public StringComponent Text { get; init; } = new("Design Item");
    [Reactive] public IconValue Icon { get; set; } = IconValues.Settings;
    public string ToolTipText { get; } = "This is design time ToolTip";

    public ReactiveCommand<NavigationInformation, Unit> NavigateCommand { get; } = 
        ReactiveCommand.Create<NavigationInformation>((info) => { });
    public bool IsActive { get; } = false;
    public bool IsSelected { get; } = false;
    public bool IsToggleVisible { get; set; } = false;
    
    public bool IsEnabled { get; set; } = true;
    public ReactiveCommand<Unit, Unit> ToggleIsEnabledCommand { get; } = ReactiveCommand.Create(() => { });
    
    public LeftMenuItemDesignViewModel()
    {
        this.WhenActivated(d => Text.Activate().DisposeWith(d));
    }
}
