using Avalonia.Media;
using JetBrains.Annotations;
using Union = OneOf.OneOf<
    NexusMods.UI.Sdk.Icons.Empty,
    NexusMods.UI.Sdk.Icons.ProjektankerIcon,
    NexusMods.UI.Sdk.Icons.AvaloniaImage,
    NexusMods.UI.Sdk.Icons.AvaloniaSvg,
    NexusMods.UI.Sdk.Icons.AvaloniaPathIcon,
    NexusMods.UI.Sdk.Icons.SimpleVectorIcon>;

namespace NexusMods.UI.Sdk.Icons;

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
    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public Union Value { get; set; }

    /// <summary>
    /// Sets the value as a <see cref="ProjektankerIcon"/>.
    /// </summary>
    public string? MdiValueSetter
    {
        set => Value = new ProjektankerIcon(value);
    }

    /// <summary>
    /// Sets the value as a <see cref="AvaloniaImage"/>.
    /// </summary>
    public IImage? ImageSetter
    {
        set => Value = new AvaloniaImage(value);
    }

    /// <summary>
    /// Sets the value as a <see cref="AvaloniaSvg"/>.
    /// </summary>
    public string? SvgSetter
    {
        set => Value = new AvaloniaSvg(value);
    }

    /// <summary>
    /// Sets the value as a <see cref="AvaloniaPathIcon"/>.
    /// </summary>
    public Geometry? GeometrySetter
    {
        set => Value = new AvaloniaPathIcon(value);
    }

    /// <summary>
    /// Sets the value as a <see cref="SimpleVectorIconImage"/>.
    /// </summary>
    public SimpleVectorIconImage SimpleVectorSetter
    {
        set => Value = new SimpleVectorIcon(value);
    }

    /// <summary>
    /// Default constructor.
    /// </summary>
    public IconValue()
    {
        Value = new Empty();
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public IconValue(Union input)
    {
        Value = input;
    }

    /// <summary/>
    public static implicit operator IconValue(ProjektankerIcon value) => new(value);
    /// <summary/>
    public static implicit operator IconValue(AvaloniaImage value) => new(value);
    /// <summary/>
    public static implicit operator IconValue(AvaloniaSvg value) => new(value);
    /// <summary/>
    public static implicit operator IconValue(AvaloniaPathIcon value) => new(value);
    /// <summary/>
    public static implicit operator IconValue(SimpleVectorIcon value) => new(value);
}

/// <summary>
/// Empty image.
/// </summary>
[PublicAPI]
public record struct Empty;

/// <summary>
/// Projectanker Icon.
/// </summary>
[PublicAPI]
public record struct ProjektankerIcon(string? Value);

/// <summary>
/// Avalonia Image.
/// </summary>
[PublicAPI]
public record struct AvaloniaImage(IImage? Image);

/// <summary>
/// Avalonia SVG.
/// </summary>
[PublicAPI]
public record struct AvaloniaSvg(string? Path);

/// <summary>
/// Avalonia path icon using geometry.
/// </summary>
[PublicAPI]
public record struct AvaloniaPathIcon(Geometry? Geometry);

/// <summary>
/// Simple vector icon.
/// </summary>
[PublicAPI]
public record struct SimpleVectorIcon(SimpleVectorIconImage Image);
