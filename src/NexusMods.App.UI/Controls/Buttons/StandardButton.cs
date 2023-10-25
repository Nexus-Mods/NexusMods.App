using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using JetBrains.Annotations;
using NexusMods.App.UI.Icons;

namespace NexusMods.App.UI.Controls.Buttons;

[PublicAPI]
[PseudoClasses(PseudoClassHasIcon)]
public class StandardButton : Button
{
    private const string PseudoClassHasIcon = ":icon";

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<StandardButton, string?>(nameof(Text));

    public static readonly StyledProperty<IconType> IconTypeProperty =
        AvaloniaProperty.Register<StandardButton, IconType>(nameof(IconType), defaultValue: IconType.None);

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value?.ToUpperInvariant());
    }

    public IconType IconType
    {
        get => GetValue(IconTypeProperty);
        set
        {
            PseudoClasses.Set(PseudoClassHasIcon, value != IconType.None);
            SetValue(IconTypeProperty, value);
        }
    }
}
