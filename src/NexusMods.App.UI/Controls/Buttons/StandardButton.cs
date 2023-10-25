using Avalonia;
using Avalonia.Controls;

namespace NexusMods.App.UI.Controls.Buttons;

public class StandardButton : Button
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<StandardButton, string?>(nameof(Text));

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
