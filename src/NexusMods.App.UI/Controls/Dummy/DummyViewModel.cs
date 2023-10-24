using Avalonia.Media;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls;

public class DummyViewModel : AViewModel<IDummyViewModel>, IDummyViewModel
{
    [Reactive] public Color Color { get; set; } = Colors.Aqua;
}
