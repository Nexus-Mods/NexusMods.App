using System.Windows.Input;
using NexusMods.App.UI.ViewModels;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.UI.Theme.Controls.Spine.Buttons;

public class SpineButtonViewModel : AViewModel
{
    [Reactive]
    public bool IsActive { get; set; }
    
    [Reactive]
    public ICommand Click { get; set; }
}