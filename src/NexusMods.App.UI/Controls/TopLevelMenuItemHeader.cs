using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using NexusMods.Icons;

namespace NexusMods.App.UI.Controls;

public class TopLevelMenuItemHeader : TemplatedControl
{
    public static readonly StyledProperty<IconValue> IconProperty = AvaloniaProperty.Register<TopLevelMenuItemHeader, IconValue>(nameof(Icon), defaultValue: new IconValue());

    public static readonly StyledProperty<string?> TextProperty = TextBlock.TextProperty.AddOwner<TopLevelMenuItemHeader>();

    public IconValue Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
