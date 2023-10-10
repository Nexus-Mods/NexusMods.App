using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;

namespace NexusMods.App.UI.Controls;

[UsedImplicitly]
public partial class DummyView : ReactiveUserControl<IDummyViewModel>
{
    public DummyView()
    {
        InitializeComponent();

        var color = Color.FromRgb(GetRandomByte(), GetRandomByte(), GetRandomByte());
        Background = new ImmutableSolidColorBrush(color);
    }

    private static byte GetRandomByte()
    {
        var i = Random.Shared.Next(byte.MinValue, byte.MaxValue);
        return (byte)i;
    }
}

