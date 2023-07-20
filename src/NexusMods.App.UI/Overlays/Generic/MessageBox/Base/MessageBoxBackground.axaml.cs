using Avalonia;
using Avalonia.Controls;

namespace NexusMods.App.UI.Overlays.Generic.MessageBox.Base;

/// <summary>
/// This provides the background 'pill' for the MessageBox class.
/// </summary>
public partial class MessageBoxBackground : UserControl
{
    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly StyledProperty<object> TopContentProperty =
        AvaloniaProperty.Register<MessageBoxBackground, object>(nameof(TopContent), new TextBlock() { Text = "Default Top Content" });
    
    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly StyledProperty<object> BottomContentProperty =
        AvaloniaProperty.Register<MessageBoxBackground, object>(nameof(BottomContent), new TextBlock() { Text = "Default Bottom Content (Long Text)" });
    
    public object TopContent
    {
        get => GetValue(TopContentProperty);
        set => SetValue(TopContentProperty, value);
    }
    
    public object BottomContent
    {
        get => GetValue(BottomContentProperty);
        set => SetValue(BottomContentProperty, value);
    }
    
    public MessageBoxBackground() => InitializeComponent();
}

