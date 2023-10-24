using Avalonia.Media;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls;

public class DummyViewModel : AViewModel<IDummyViewModel>, IDummyViewModel
{
    [Reactive] public Color Color { get; set; } = Color.FromRgb(GetRandomByte(), GetRandomByte(), GetRandomByte());

    private static byte GetRandomByte()
    {
        var i = Random.Shared.Next(byte.MinValue, byte.MaxValue);
        return (byte)i;
    }
}
