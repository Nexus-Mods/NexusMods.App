using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.Controls;

[PseudoClasses(":active", ":icon")]
public class EmptyState : TemplatedControl
{
    private TextBlock? _header;
    private UnifiedIcon? _icon;
    
    public static readonly StyledProperty<bool> IsActiveProperty = AvaloniaProperty.Register<EmptyState, bool>(nameof(IsActive));

    public static readonly StyledProperty<IconValue?> IconProperty = AvaloniaProperty.Register<EmptyState, IconValue?>(nameof(Icon));

    public static readonly StyledProperty<string> HeaderProperty = AvaloniaProperty.Register<EmptyState, string>(nameof(Header), defaultValue: string.Empty);

    public static readonly StyledProperty<object?> SubtitleProperty = AvaloniaProperty.Register<EmptyState, object?>(nameof(Subtitle));

    public static readonly StyledProperty<object?> ContentProperty = AvaloniaProperty.Register<EmptyState, object?>(nameof(Content));

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public IconValue? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public object? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == IsActiveProperty)
        {
            PseudoClasses.Remove(":active");

            if (change.NewValue is true)
            {
                PseudoClasses.Add(":active");
            }
        } 
        else if (change.Property == IconProperty)
        {
            PseudoClasses.Remove(":icon");

            if (change.NewValue is IconValue)
            {
                PseudoClasses.Add(":icon");
            }
            
            UpdateIconVisibility();
        }
        else if (change.Property == HeaderProperty)
        {
            UpdateHeaderVisibility();
        }

        base.OnPropertyChanged(change);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _header = e.NameScope.Find<TextBlock>("HeaderTextBlock");
        _icon = e.NameScope.Find<UnifiedIcon>("Icon");
        
        if (_header is not null)
            UpdateHeaderVisibility();

        if (_icon is not null)
            UpdateIconVisibility();
        
        base.OnApplyTemplate(e);
    }
    
    private void UpdateHeaderVisibility()
    {
        if (_header is not null)
            _header.IsVisible = !string.IsNullOrEmpty(Header);
    }
    
    private void UpdateIconVisibility()
    {
        if (_icon is not null)
            _icon.IsVisible = Icon is not null;
    }
}
