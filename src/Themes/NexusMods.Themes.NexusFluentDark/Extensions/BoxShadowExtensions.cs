using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace NexusMods.Themes.NexusFluentDark.Extensions;

/// <summary>
/// Xaml extension for BoxShadow.
/// This allows setting BoxShadow with a resource for the Color instead of a hardcoded hex value.
/// </summary>
/// <example>
///     <Border BoxShadow="{extensions:BoxShadow BlurRadius=2, SpreadRadius=2, ShadowColor={DynamicResource AccentColor}, IsInset=False}"/>
/// </example>
public class BoxShadowExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var bx = new BoxShadow()
        {
            IsInset = IsInset,
            Blur = BlurRadius,
            Spread = SpreadRadius,
            Color = ShadowColor,
            OffsetX = HorizontalLength,
            OffsetY = VerticalLength
        };
        return new BoxShadows(bx);
    }

    public double HorizontalLength { get; set; }

    public double VerticalLength { get; set; }

    public double BlurRadius { get; set; }

    public double SpreadRadius { get; set; }

    public Color ShadowColor { get; set; }

    public bool IsInset { get; set; }
}

