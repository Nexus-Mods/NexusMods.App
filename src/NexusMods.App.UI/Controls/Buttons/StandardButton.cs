using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Media;
using JetBrains.Annotations;
using NexusMods.App.UI.Icons;

namespace NexusMods.App.UI.Controls.Buttons;

[PublicAPI]
[PseudoClasses(PseudoClassHasIcon, PseudoClassHasImage)]
public class StandardButton : Button
{
    private const string PseudoClassHasIcon = ":icon";
    private const string PseudoClassHasImage = ":image";

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<StandardButton, string?>(nameof(Text));

    public static readonly StyledProperty<IconType> IconTypeProperty =
        AvaloniaProperty.Register<StandardButton, IconType>(nameof(IconType), defaultValue: IconType.None);

    public static readonly StyledProperty<IImage?> ImageProperty =
        AvaloniaProperty.Register<StandardButton, IImage?>(nameof(Image), defaultValue: null);

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value?.ToUpperInvariant());
    }

    public IconType IconType
    {
        get => GetValue(IconTypeProperty);
        set => SetValue(IconTypeProperty, value);
    }

    public IImage? Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(PseudoClassHasIcon, IconType != IconType.None);
        PseudoClasses.Set(PseudoClassHasImage, Image is not null);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        UpdatePseudoClasses();
        base.OnPropertyChanged(change);
    }
}
