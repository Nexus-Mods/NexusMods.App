using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using JetBrains.Annotations;

namespace NexusMods.Themes.NexusFluentDark.Extensions;

/// <summary>
/// Xaml extension for Thickness (used by Margin, Padding, BorderThickness).
/// This allows setting a Thickness using a resource for each side.
/// </summary>
/// <example>
///     <Border Padding="{extensions:Thickness Left={StaticResource Spacing-2}, Top=0, Right={StaticResource Spacing-2}, Bottom=0}"/>
/// </example>
public class ThicknessExtension: MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new Thickness(Left, Top, Right, Bottom);
    }

    public double Left { get; set; }
    public double Top { get; set; }
    public double Right { get; set; }
    public double Bottom { get; set; }
}
