using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using JetBrains.Annotations;

namespace NexusMods.UI.Sdk.Icons;

/// <summary>
/// Unified icon class that supports <see cref="Projektanker.Icons.Avalonia.Icon"/>,
/// <see cref="Avalonia.Controls.Image"/>, <see cref="Avalonia.Svg.Skia.Svg"/>, and
/// <see cref="Avalonia.Controls.PathIcon"/>.
/// </summary>
[PublicAPI]
public sealed class UnifiedIcon : ContentControl
{
    private const double DefaultSize = 24;
    
    /// <inheritdoc cref="Value"/>
    public static readonly StyledProperty<IconValue?> ValueProperty = AvaloniaProperty.Register<UnifiedIcon, IconValue?>(nameof(Value));

    /// <inheritdoc cref="Size"/>
    public static readonly StyledProperty<double> SizeProperty = AvaloniaProperty.Register<UnifiedIcon, double>(nameof(Size), defaultValue: DefaultSize);

    /// <inheritdoc cref="MaxSize"/>
    public static readonly StyledProperty<double> MaxSizeProperty = AvaloniaProperty.Register<UnifiedIcon, double>(nameof(MaxSize));

    // NOTE(erri120): The Svg control needs a "baseUri", however, I don't think this does anything.
    private static readonly Uri Default = new("https://example.org");

    /// <summary>
    /// Gets or sets the icon value.
    /// </summary>
    public IconValue? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    /// Gets or sets the size of the icon.
    /// </summary>
    /// <remarks>
    /// This sets <c>Height</c>, <c>Width</c>, and <c>FontSize</c>.
    /// </remarks>
    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the max size of the icon.
    /// </summary>
    /// <remarks>
    /// This sets <c>MaxHeight</c>, and <c>MaxWidth</c>.
    /// </remarks>
    public double MaxSize
    {
        get => GetValue(MaxSizeProperty);
        set => SetValue(MaxSizeProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
        {
            UpdateControl(change.NewValue as IconValue);
        } else if (change.Property == SizeProperty)
        {
            if (change.NewValue is null) return;
            var value = (double)change.NewValue;

            Height = value;
            Width = value;
            FontSize = value;
        } else if (change.Property == MaxSizeProperty)
        {
            if (change.NewValue is null) return;
            var value = (double)change.NewValue;

            MaxHeight = value;
            MaxWidth = value;
        } else if (change.Property == ForegroundProperty && Content is PathIcon pathIcon)
        {
            // Note(Al12rs): workaround to update PathIcon foreground colors
            pathIcon.Foreground = (IBrush?)change.NewValue;
        }
    }

    [SuppressMessage("ReSharper", "RedundantNameQualifier")]
    private void UpdateControl(IconValue? value)
    {
        if (value is null)
        {
            Content = null;
            return;
        }

        var union = value.Value;
        var innerControl = union.Match<Control?>(
            f0: _ => null,
            f1: projektankerIcon => new Projektanker.Icons.Avalonia.Icon
            {
                Value = projektankerIcon.Value ?? string.Empty,
            },
            f2: avaloniaImage => new Avalonia.Controls.Image
            {
                Source = avaloniaImage.Image,
            },
            f3: avaloniaSvg => new Avalonia.Svg.Skia.Svg(baseUri: Default)
            {
                Path = avaloniaSvg.Path,
            },
            f4: avaloniaPathIcon => new Avalonia.Controls.PathIcon
            {
                Data = avaloniaPathIcon.Geometry ?? new LineGeometry(),
                // NOTE(erri120): bind our Height and Width properties to their properties
                // otherwise the icon won't be affected by our dimensions
                [HeightProperty] = this[HeightProperty],
                [WidthProperty] = this[WidthProperty],
                [MaxHeightProperty] = this[MaxHeightProperty],
                [MaxWidthProperty] = this[MaxWidthProperty],
                [ForegroundProperty] = this[ForegroundProperty],
            },
            f5: simpleVectorIcon => new SimpleVectorIconControl(simpleVectorIcon.Image.Clone())
        );

        if (innerControl is null) return;
        Content = innerControl;
    }

    /// <inheritdoc/>
    protected override bool BypassFlowDirectionPolicies => true;
}
