using System.Windows.Input;
using NexusMods.App.UI.ViewModels;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Spine.Buttons;

public abstract class SpineButtonViewModel : AViewModel<ISpineButtonViewModel>
{
    [Reactive]
    public bool IsActive { get; set; }
    
    [Reactive]
    public ICommand Click { get; set; }
}