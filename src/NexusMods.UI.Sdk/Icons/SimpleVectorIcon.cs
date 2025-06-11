using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace NexusMods.UI.Sdk.Icons;

/// <summary>
///     A wrapper for <see cref="Avalonia.Controls.Image"/> designed to work with <see cref="SimpleVectorIconImage"/>.
/// </summary>
public class SimpleVectorIconControl : Avalonia.Controls.Image
{
    /// <summary>
    /// Defines the <see cref="Foreground"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<SimpleVectorIconControl>();
    
    /// <summary>
    /// Gets or sets the brush used to draw the control's text and other foreground elements.
    /// </summary>
    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }
    
    /// <inheritdoc />
    public SimpleVectorIconControl(SimpleVectorIconImage? image) => Source = image;

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ForegroundProperty)
        {
            ((SimpleVectorIconImage)Source!).Brush = Foreground;
        }
    }
}
