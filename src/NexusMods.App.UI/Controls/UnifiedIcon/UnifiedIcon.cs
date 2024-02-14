using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using JetBrains.Annotations;

namespace NexusMods.App.UI.Controls.UnifiedIcon;

/// <summary>
/// Unified icon class that supports <see cref="Projektanker.Icons.Avalonia.Icon"/>,
/// <see cref="Avalonia.Controls.Image"/>, <see cref="Avalonia.Svg.Skia.Svg"/>, and
/// <see cref="Avalonia.Controls.PathIcon"/>.
/// </summary>
[PublicAPI]
public sealed class UnifiedIcon : ContentControl
{
    public static readonly StyledProperty<IconValue?> ValueProperty = AvaloniaProperty
        .Register<UnifiedIcon, IconValue?>(nameof(Value));

    public static readonly StyledProperty<double> SizeProperty = AvaloniaProperty
        .Register<UnifiedIcon, double>(nameof(Size));

    public static readonly StyledProperty<double> MaxSizeProperty = AvaloniaProperty
        .Register<UnifiedIcon, double>(nameof(MaxSize));

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
                Value = projektankerIcon.Value ?? string.Empty
            },
            f2: avaloniaImage => new Avalonia.Controls.Image
            {
                Source = avaloniaImage.Image
            },
            f3: avaloniaSvg => new Avalonia.Svg.Skia.Svg(baseUri: Default)
            {
                Path = avaloniaSvg.Path
            },
            f4: avaloniaPathIcon => new Avalonia.Controls.PathIcon
            {
                Data = avaloniaPathIcon.Data ?? new LineGeometry(),
                // NOTE(erri120): bind our Height and Width properties to their properties
                // otherwise the icon won't be affected by our dimensions
                [HeightProperty] = this[HeightProperty],
                [WidthProperty] = this[WidthProperty],
                [MaxHeightProperty] = this[MaxHeightProperty],
                [MaxWidthProperty] = this[MaxWidthProperty]
            }
        );

        if (innerControl is null) return;
        Content = innerControl;
    }

    /// <inheritdoc/>
    protected override bool BypassFlowDirectionPolicies => true;
}
