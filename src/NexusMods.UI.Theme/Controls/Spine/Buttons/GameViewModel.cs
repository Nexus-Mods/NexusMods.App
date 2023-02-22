using System.Windows.Input;
using Avalonia.Media;
using NexusMods.App.UI.ViewModels;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.UI.Theme.Controls.Spine.Buttons;

public class GameViewModel : AViewModel, ISpineButtonViewModel
{
    [Reactive]
    public bool IsActive { get; set; }
    
    [Reactive]
    public string Name { get; set; }
    
    [Reactive]
    public IImage Image { get; set; }
    
    [Reactive]
    public ICommand Click { get; set; }
}