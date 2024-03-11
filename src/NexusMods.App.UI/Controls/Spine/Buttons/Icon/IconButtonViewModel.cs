using System.Windows.Input;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Icon;

public class IconButtonViewModel : AViewModel<IIconButtonViewModel>, IIconButtonViewModel
{
    [Reactive]
    public bool IsActive { get; set; }

    [Reactive] public ICommand Click { get; set; } = Initializers.ICommand;
    
    public IWorkspaceContext? WorkspaceContext { get; set; }

    [Reactive] public string Name { get; set; } = string.Empty;
}
