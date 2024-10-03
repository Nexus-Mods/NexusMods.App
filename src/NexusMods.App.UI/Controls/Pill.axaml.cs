using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using NexusMods.Icons;

namespace NexusMods.App.UI.Controls;

public class Pill : TemplatedControl
{
    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<Pill, string?>(nameof(Text), defaultValue: "Default");
    public static readonly StyledProperty<IconValue?> IconProperty = AvaloniaProperty.Register<Pill, IconValue?>(nameof(Icon), defaultValue: IconValues.Star);
    public static readonly AttachedProperty<bool> ShowIconProperty = AvaloniaProperty.RegisterAttached<Pill, TemplatedControl, bool>("ShowIcon", defaultValue: true);

    private UnifiedIcon? _icon  = null;
    
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    
    public IconValue? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
    
    public bool ShowIcon
    {
        get => GetValue(ShowIconProperty);
        set => SetValue(ShowIconProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        
        _icon = e.NameScope.Find<UnifiedIcon>("PillIcon");
        
        if (_icon == null) return;

        _icon.Value = Icon;
        _icon.IsVisible = ShowIcon;
    }
}
