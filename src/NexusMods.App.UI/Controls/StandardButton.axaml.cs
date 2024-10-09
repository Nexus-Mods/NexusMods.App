using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using NexusMods.Icons;

namespace NexusMods.App.UI.Controls;

[TemplatePart("PART_LeftIcon",  typeof(UnifiedIcon))]
[TemplatePart("PART_RightIcon", typeof(UnifiedIcon))]
[TemplatePart("PART_Label", typeof(TextBlock))]
public class StandardButton : Button
{
    //protected override Type StyleKeyOverride { get; } = typeof(Button);

    public enum VisibleIcons { None, Left, Right, Both, }
    public enum Sizes { Medium, Small, }
    public enum Types { None, Primary, Secondary, Tertiary, }
    public enum Fills { None, Strong, Weak, }

    private UnifiedIcon? _leftIcon  = null;
    private UnifiedIcon? _rightIcon = null;
    private TextBlock? _label = null;
    private ContentPresenter? _content = null;
    private Border? _border = null;
    
    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<StandardButton, string?>(nameof(Text), defaultValue: "Standard Button");
    public static readonly StyledProperty<IconValue?> LeftIconProperty = AvaloniaProperty.Register<StandardButton, IconValue?>(nameof(LeftIcon), defaultValue: IconValues.ChevronDown);
    public static readonly StyledProperty<IconValue?> RightIconProperty = AvaloniaProperty.Register<StandardButton, IconValue?>(nameof(RightIcon), defaultValue: IconValues.ChevronUp);
    
    public static readonly AttachedProperty<VisibleIcons> VisibleIconProperty = AvaloniaProperty.RegisterAttached<StandardButton, TemplatedControl, VisibleIcons>("VisibleIcon", defaultValue: VisibleIcons.None);
    public static readonly AttachedProperty<Types> TypeProperty = AvaloniaProperty.RegisterAttached<StandardButton, TemplatedControl, Types>("Type", defaultValue: Types.None);
    public static readonly AttachedProperty<Sizes> SizeProperty = AvaloniaProperty.RegisterAttached<StandardButton, TemplatedControl, Sizes>("Size", defaultValue: Sizes.Medium);
    public static readonly AttachedProperty<Fills> FillProperty = AvaloniaProperty.RegisterAttached<StandardButton, TemplatedControl, Fills>("Fill", defaultValue: Fills.None);
    public static readonly AttachedProperty<bool> ShowLabelProperty = AvaloniaProperty.RegisterAttached<StandardButton, TemplatedControl, bool>("ShowLabel", defaultValue: true);
    
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    
    public VisibleIcons? VisibleIcon
    {
        get => GetValue(VisibleIconProperty);
        set => SetValue(VisibleIconProperty, value);
    }
    
    public IconValue? LeftIcon
    {
        get => GetValue(LeftIconProperty);
        set => SetValue(LeftIconProperty, value);
    }
    
    public IconValue? RightIcon
    {
        get => GetValue(RightIconProperty);
        set => SetValue(RightIconProperty, value);
    }
    
    public bool ShowLabel
    {
        get => GetValue(ShowLabelProperty);
        set => SetValue(ShowLabelProperty, value);
    }
    
    public Types Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }
    
    public Sizes Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }
    
    public Fills Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _leftIcon = e.NameScope.Find<UnifiedIcon>("PART_LeftIcon");
        _rightIcon = e.NameScope.Find<UnifiedIcon>("PART_RightIcon");
        _label = e.NameScope.Find<TextBlock>("PART_Label");
        _content = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
        _border = e.NameScope.Find<Border>("PART_Border");

        if (_leftIcon == null || _rightIcon == null || _label == null || _content == null || _border == null) return;

        _leftIcon.Value = LeftIcon;
        _rightIcon.Value = RightIcon;

        _label.IsVisible = ShowLabel;

        // so we can use the traditional button as well as our own properties to set the button
        if (Content is null)
        {
            _content.IsVisible = false;
            _border.IsVisible = true;
        }
        else
        {
            _content.IsVisible = true;
            _border.IsVisible = false;
        }

        switch (VisibleIcon)
        {
            case VisibleIcons.None:
                _leftIcon!.IsVisible = false;
                _rightIcon!.IsVisible = false;
                break;
            case VisibleIcons.Left:
                _leftIcon!.IsVisible = true;
                _rightIcon!.IsVisible = false;
                break;
            case VisibleIcons.Right:
                _leftIcon!.IsVisible = false;
                _rightIcon!.IsVisible = true;
                break;
            case VisibleIcons.Both:
                _leftIcon!.IsVisible = true;
                _rightIcon!.IsVisible = true;
                break;
            default:
                _leftIcon!.IsVisible = false;
                _rightIcon!.IsVisible = false;
                break;
        }
    }
}

