using System.Windows.Input;
using Avalonia.Media;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Image;

public class ImageButtonViewModel : AViewModel<IImageButtonViewModel>, IImageButtonViewModel
{
    [Reactive]
    public bool IsActive { get; set; }

    [Reactive] public string Name { get; set; } = "";

    [Reactive] public IImage Image { get; set; } = Initializers.IImage;

    [Reactive] public ICommand Click { get; set; } = Initializers.ICommand;

    [Reactive]
    public object? Tag { get; set; }
}
