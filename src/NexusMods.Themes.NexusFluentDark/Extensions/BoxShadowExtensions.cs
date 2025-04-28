using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace NexusMods.Themes.NexusFluentDark.Extensions;

/// <summary>
/// Xaml extension for BoxShadow.
/// This allows setting BoxShadow with a resource for the Color instead of a hardcoded hex value.
/// It is also possible to set multiple BoxShadows on a control, following the same l,t,r,b order.
/// </summary>
/// <example>
///     <Border BoxShadow="{extensions:BoxShadow BlurRadius=2, SpreadRadius=2, ShadowColor={DynamicResource AccentColor}, IsInset=False}"/>
/// </example>
public class BoxShadowsExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var bx = new BoxShadow()
        {
            IsInset = IsInset,
            OffsetX = HorizontalLength,
            OffsetY = VerticalLength,
            Blur = BlurRadius,
            Spread = SpreadRadius,
            Color = ShadowColor,
        };

        if (ShadowColor1 == default(Color) && ShadowColor2 == default(Color) && ShadowColor3 == default(Color))
            return new BoxShadows(bx); 
        
        var bxArray = new BoxShadow[3];

        var bx1 = new BoxShadow()
        {
            IsInset = IsInset1,
            Blur = BlurRadius1,
            Spread = SpreadRadius1,
            Color = ShadowColor1,
            OffsetX = HorizontalLength1,
            OffsetY = VerticalLength1,
        };
        bxArray[0] = bx1;

        var bx2 = new BoxShadow()
        {
            IsInset = IsInset2,
            Blur = BlurRadius2,
            Spread = SpreadRadius2,
            Color = ShadowColor2,
            OffsetX = HorizontalLength2,
            OffsetY = VerticalLength2,
        };
        bxArray[1] = bx2;

        var bx3 = new BoxShadow()
        {
            IsInset = IsInset3,
            Blur = BlurRadius3,
            Spread = SpreadRadius3,
            Color = ShadowColor3,
            OffsetX = HorizontalLength3,
            OffsetY = VerticalLength3,
        };
        bxArray[2] = bx3;

        return new BoxShadows(bx, bxArray);
    }

    public bool IsInset { get; set; }
    public double HorizontalLength { get; set; }
    public double VerticalLength { get; set; }
    public double BlurRadius { get; set; }
    public double SpreadRadius { get; set; }
    public Color ShadowColor { get; set; }


    public double HorizontalLength1 { get; set; }
    public double VerticalLength1 { get; set; }
    public double BlurRadius1 { get; set; }
    public double SpreadRadius1 { get; set; }
    public Color ShadowColor1 { get; set; }
    public bool IsInset1 { get; set; }

    public double HorizontalLength2 { get; set; }
    public double VerticalLength2 { get; set; }
    public double BlurRadius2 { get; set; }
    public double SpreadRadius2 { get; set; }
    public Color ShadowColor2 { get; set; }
    public bool IsInset2 { get; set; }

    public double HorizontalLength3 { get; set; }
    public double VerticalLength3 { get; set; }
    public double BlurRadius3 { get; set; }
    public double SpreadRadius3 { get; set; }
    public Color ShadowColor3 { get; set; }
    public bool IsInset3 { get; set; }
}

