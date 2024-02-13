using Avalonia.Media;
using JetBrains.Annotations;
using Union = OneOf.OneOf<
    NexusMods.App.UI.Controls.GenericIcon.Empty,
    NexusMods.App.UI.Controls.GenericIcon.ProjektankerIcon,
    NexusMods.App.UI.Controls.GenericIcon.AvaloniaImage,
    NexusMods.App.UI.Controls.GenericIcon.AvaloniaSvg,
    NexusMods.App.UI.Controls.GenericIcon.AvaloniaPathIcon>;

namespace NexusMods.App.UI.Controls.GenericIcon;

/// <summary>
/// Represents a union between
/// <see cref="Empty"/>,
/// <see cref="ProjektankerIcon"/>,
/// <see cref="AvaloniaImage"/>,
/// <see cref="AvaloniaSvg"/>, and
/// <see cref="AvaloniaPathIcon"/>.
/// </summary>
/// <seealso cref="GenericIcon"/>
[PublicAPI]
public sealed class IconValue
{
    public Union Value { get; set; }

    public ProjektankerIcon ProjektankerIconValueSetter
    {
        set => Value = value;
    }

    public AvaloniaImage AvaloniaImageSetter
    {
        set => Value = value;
    }

    public AvaloniaSvg AvaloniaSvgSetter
    {
        set => Value = value;
    }

    public AvaloniaPathIcon AvaloniaPathIconSetter
    {
        set => Value = value;
    }

    public IconValue()
    {
        Value = new Empty();
    }

    public IconValue(Union input)
    {
        Value = input;
    }
}

public record Empty;

public class ProjektankerIcon
{
    public string? Value { get; set; }
}

public class AvaloniaImage
{
    public IImage? Image { get; set; }
}

public class AvaloniaSvg
{
    public string? Path { get; set; }
}

public class AvaloniaPathIcon
{
    public Geometry? Data { get; set; }
}
