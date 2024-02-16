using Avalonia.Media;
using JetBrains.Annotations;
using Union = OneOf.OneOf<
    NexusMods.App.UI.Controls.UnifiedIcon.Empty,
    NexusMods.App.UI.Controls.UnifiedIcon.ProjektankerIcon,
    NexusMods.App.UI.Controls.UnifiedIcon.AvaloniaImage,
    NexusMods.App.UI.Controls.UnifiedIcon.AvaloniaSvg,
    NexusMods.App.UI.Controls.UnifiedIcon.AvaloniaPathIcon>;

namespace NexusMods.App.UI.Controls.UnifiedIcon;

/// <summary>
/// Represents a union between
/// <see cref="Empty"/>,
/// <see cref="ProjektankerIcon"/>,
/// <see cref="AvaloniaImage"/>,
/// <see cref="AvaloniaSvg"/>, and
/// <see cref="AvaloniaPathIcon"/>.
/// </summary>
/// <seealso cref="UnifiedIcon"/>
[PublicAPI]
public sealed class IconValue
{
    public Union Value { get; set; }

    public string? MdiValueSetter
    {
        set => Value = new ProjektankerIcon(value);
    }

    public IImage? ImageSetter
    {
        set => Value = new AvaloniaImage(value);
    }

    public string? SvgSetter
    {
        set => Value = new AvaloniaSvg(value);
    }

    public Geometry? GeometrySetter
    {
        set => Value = new AvaloniaPathIcon(value);
    }

    public IconValue()
    {
        Value = new Empty();
    }

    public IconValue(Union input)
    {
        Value = input;
    }

    public static implicit operator IconValue(ProjektankerIcon value) => new(value);
    public static implicit operator IconValue(AvaloniaImage value) => new(value);
    public static implicit operator IconValue(AvaloniaSvg value) => new(value);
    public static implicit operator IconValue(AvaloniaPathIcon value) => new(value);
}

[PublicAPI]
public record Empty;

[PublicAPI]
public record ProjektankerIcon(string? Value);

[PublicAPI]
public record AvaloniaImage(IImage? Image);

[PublicAPI]
public record AvaloniaSvg(string? Path);

[PublicAPI]
public record AvaloniaPathIcon(Geometry? Geometry);

