using Avalonia;
using Avalonia.Media;
using JetBrains.Annotations;

namespace NexusMods.UI.Sdk.Icons;

/// <summary>
/// Represents a simple SVG icon image that can be used in Avalonia applications.
/// </summary>
[PublicAPI]
public class SimpleVectorIconImage : DrawingImage, IImage
{
    private readonly Rect _viewBox;
    private readonly GeometryDrawing _drawing;

    /// <summary>
    /// Defines the <see cref="Brush"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> BrushProperty = AvaloniaProperty.Register<
        SimpleVectorIconImage,
        IBrush?
    >(nameof(Brush), null);

    /// <summary>
    /// Defines the <see cref="Pen"/> property.
    /// </summary>
    public static readonly StyledProperty<IPen?> PenProperty = AvaloniaProperty.Register<
        SimpleVectorIconImage,
        IPen?
    >(nameof(Pen), null);

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleVectorIconImage"/> class with specified path data, view box, brush, and pen.
    /// </summary>
    /// <param name="pathData">The SVG path data for the icon.</param>
    /// <param name="viewBox">The view box of the SVG icon.</param>
    /// <param name="brush">The brush used to fill the icon. Can be null for no fill.</param>
    /// <param name="pen">The pen used to stroke the icon. Can be null for no stroke.</param>
    public SimpleVectorIconImage(string pathData, Rect viewBox, IBrush? brush = null, IPen? pen = null)
    {
        _viewBox = viewBox;

        /*
            TODO(Sewer): Write a TinyVG parser that directly feeds into StreamGeometry.
            And pass the path as a byte span. This way we can avoid slow string parsing
            and save on binary size. Will pick this up in my own time. Shouldn't be that hard.

            Also considered parsing out the SVG and feeding StreamGeometry commands
            directly via source generator. However that just bloats the code, as
            .NET is unable to compile time generate the resulting StreamGeometry.
        */
        _drawing = new GeometryDrawing
        {
            Geometry = StreamGeometry.Parse(pathData),
            Brush = brush,
            Pen = pen,
        };

        Drawing = _drawing;
        Brush = brush;
        Pen = pen;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleVectorIconImage"/> class with specified geometry, view box, brush, and pen.
    /// </summary>
    /// <param name="geometry">The StreamGeometry for the icon.</param>
    /// <param name="viewBox">The view box of the SVG icon.</param>
    /// <param name="brush">The brush used to fill the icon. Can be null for no fill.</param>
    /// <param name="pen">The pen used to stroke the icon. Can be null for no stroke.</param>
    public SimpleVectorIconImage(StreamGeometry geometry, Rect viewBox, IBrush? brush = null, IPen? pen = null)
    {
        _viewBox = viewBox;

        _drawing = new GeometryDrawing
        {
            Geometry = geometry,
            Brush = brush,
            Pen = pen,
        };

        Drawing = _drawing;
        Brush = brush;
        Pen = pen;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleVectorIconImage"/> class with specified path data and view box, using a black brush for fill.
    /// </summary>
    /// <param name="pathData">The SVG path data for the icon.</param>
    /// <param name="viewBox">The view box of the SVG icon.</param>
    public SimpleVectorIconImage(string pathData, Rect viewBox)
        : this(pathData, viewBox, new SolidColorBrush(0xFFFFFFFF))
    {
    }
    
    /// <summary>
    /// Gets the view box of the SVG icon.
    /// </summary>
    public Rect ViewBox => _viewBox;

    /// <summary>
    /// Gets or sets the brush used to fill the icon. Can be null for no fill.
    /// </summary>
    public IBrush? Brush
    {
        get => GetValue(BrushProperty);
        set => SetValue(BrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the pen used to stroke the icon. Can be null for no stroke.
    /// </summary>
    public IPen? Pen
    {
        get => GetValue(PenProperty);
        set => SetValue(PenProperty, value);
    }

    /// <summary>
    /// Gets the size of the icon.
    /// </summary>
    public new Size Size => _viewBox.Size;

    /// <inheritdoc/>
    Size IImage.Size => _viewBox.Size;

    /// <summary>
    /// Called when a property value changes.
    /// </summary>
    /// <param name="change">A description of the property change.</param>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BrushProperty)
        {
            _drawing.Brush = Brush;
            RaiseInvalidated(EventArgs.Empty);
        }
        else if (change.Property == PenProperty)
        {
            _drawing.Pen = Pen;
            RaiseInvalidated(EventArgs.Empty);
        }
    }

    /// <inheritdoc/>
    void IImage.Draw(DrawingContext context, Rect sourceRect, Rect destRect)
    {
        var bounds = _viewBox;
        var scale = Matrix.CreateScale(
            destRect.Width / sourceRect.Width,
            destRect.Height / sourceRect.Height
        );
        var translate = Matrix.CreateTranslation(
            -sourceRect.X + destRect.X - bounds.X,
            -sourceRect.Y + destRect.Y - bounds.Y
        );

        using var clip = context.PushClip(destRect);
        using var state = context.PushTransform(translate * scale);
        _drawing.Draw(context);
    }

    /// <summary>
    /// Creates a new SimpleVectorIconImage that is a copy of the current instance.
    /// </summary>
    /// <returns>A new SimpleVectorIconImage with the same properties as this instance.</returns>
    /// <remarks>
    ///     The purpose of this method is to create a distinct instance of the
    ///     current object whose visual properties (such as colour) can be modified
    ///     without affecting the original object.
    ///
    ///     (Assuming, that the properties, such as brushes are mutated by assigning
    ///     a new instance to the property, which is standard for XAML-like frameworks)
    ///
    ///     The actual geometry of the icon is reused, thus we skip the parsing
    ///     overhead.
    /// </remarks>
    public SimpleVectorIconImage Clone()
    {
        return new SimpleVectorIconImage(
            (_drawing.Geometry as StreamGeometry)!,
            _viewBox,
            Brush,
            Pen
        );
    }
}
