using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using JetBrains.Annotations;

namespace NexusMods.App.UI.Controls.GenericIcon;

/// <summary>
/// Generic icon class that supports <see cref="Projektanker.Icons.Avalonia.Icon"/>,
/// <see cref="Avalonia.Controls.Image"/>, <see cref="Avalonia.Svg.Skia.Svg"/>, and
/// <see cref="Avalonia.Controls.PathIcon"/>.
/// </summary>
[PublicAPI]
public sealed class GenericIcon : ContentControl
{
    public static readonly StyledProperty<IconValue?> ValueProperty = AvaloniaProperty
        .Register<GenericIcon, IconValue?>(nameof(Value));

    private static readonly Uri Default = new("https://example.org");

    public IconValue? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
        {
            UpdateControl(change.NewValue as IconValue);
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
                Data = avaloniaPathIcon.Data ?? new LineGeometry()
            }
        );

        if (innerControl is null) return;
        Content = innerControl;
    }

    /// <inheritdoc/>
    protected override bool BypassFlowDirectionPolicies => true;
}
