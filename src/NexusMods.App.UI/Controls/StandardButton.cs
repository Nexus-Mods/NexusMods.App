using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using NexusMods.Icons;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// A (Nexus Mods) Standard Button control.
/// </summary>
[TemplatePart("PART_LeftIcon", typeof(UnifiedIcon))]
[TemplatePart("PART_RightIcon", typeof(UnifiedIcon))]
[TemplatePart("PART_Label", typeof(TextBlock))]
public class StandardButton : Button
{
    #region Enums
    
    /// <summary>
    /// Defines what icons are shown on a <see cref="StandardButton"/>.
    /// </summary>
    public enum ShowIconOptions
    {
        /// <summary>
        /// No icon is displayed.
        /// </summary>
        None,

        /// <summary>
        /// Icon is displayed on the left.
        /// </summary>
        Left,

        /// <summary>
        /// Icon is displayed on the right.
        /// </summary>
        Right,

        /// <summary>
        /// Icons are displayed on both sides.
        /// </summary>
        Both,  
        
        /// <summary>
        /// Single icon is displayed with zero padding and no label.
        /// </summary>
        IconOnly,
    }

    /// <summary>
    /// Defines the sizes of a <see cref="StandardButton"/>.
    /// </summary>
    public enum Sizes
    {
        /// <summary>
        /// Medium size.
        /// </summary>
        Medium,

        /// <summary>
        /// Small size.
        /// </summary>
        Small,
    }

    /// <summary>
    /// Defines the types of a <see cref="StandardButton"/>.
    /// </summary>
    public enum Types
    {
        /// <summary>
        /// No specific type.
        /// </summary>
        None,

        /// <summary>
        /// Primary type.
        /// </summary>
        Primary,

        /// <summary>
        /// Secondary type.
        /// </summary>
        Secondary,

        /// <summary>
        /// Tertiary type.
        /// </summary>
        Tertiary,
    }

    /// <summary>
    /// Defines the fill options of a <see cref="StandardButton"/>.
    /// </summary>
    public enum Fills
    {
        /// <summary>
        /// No fill.
        /// </summary>
        None,

        /// <summary>
        /// Strong fill.
        /// </summary>
        Strong,

        /// <summary>
        /// Weak fill (translucent).
        /// </summary>
        Weak,
        
        /// <summary>
        /// Weak solid fill.
        /// </summary>
        WeakAlt,
    }
    
    #endregion

    private UnifiedIcon? _leftIcon = null;
    private UnifiedIcon? _rightIcon = null;
    private TextBlock? _label = null;
    private ContentPresenter? _content = null;
    private Border? _border = null;

    /// <summary>
    /// Defines the Text property of the <see cref="StandardButton"/>.
    /// </summary>
    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<StandardButton, string?>(nameof(Text), defaultValue: "Standard Button");

    /// <summary>
    /// Defines the LeftIcon property of the <see cref="StandardButton"/>.
    /// </summary>
    public static readonly StyledProperty<IconValue?> LeftIconProperty = AvaloniaProperty.Register<StandardButton, IconValue?>(nameof(LeftIcon), defaultValue: IconValues.Add);

    /// <summary>
    /// Defines the RightIcon property of the <see cref="StandardButton"/>.
    /// </summary>
    public static readonly StyledProperty<IconValue?> RightIconProperty = AvaloniaProperty.Register<StandardButton, IconValue?>(nameof(RightIcon), defaultValue: IconValues.PlayArrow);

    /// <summary>
    /// Defines the ShowIcon attached property of the <see cref="StandardButton"/>.
    /// </summary>
    public static readonly AttachedProperty<ShowIconOptions> ShowIconProperty =
        AvaloniaProperty.RegisterAttached<StandardButton, TemplatedControl, ShowIconOptions>("ShowIcon", defaultValue: ShowIconOptions.None);

    /// <summary>
    /// Defines the Type attached property of the <see cref="StandardButton"/>.
    /// </summary>
    public static readonly AttachedProperty<Types> TypeProperty = AvaloniaProperty.RegisterAttached<StandardButton, TemplatedControl, Types>("Type", defaultValue: Types.Tertiary);

    /// <summary>
    /// Defines the Size attached property of the <see cref="StandardButton"/>.
    /// </summary>
    public static readonly AttachedProperty<Sizes> SizeProperty = AvaloniaProperty.RegisterAttached<StandardButton, TemplatedControl, Sizes>("Size", defaultValue: Sizes.Medium);

    /// <summary>
    /// Defines the Fill attached property of the <see cref="StandardButton"/>.
    /// </summary>
    public static readonly AttachedProperty<Fills> FillProperty = AvaloniaProperty.RegisterAttached<StandardButton, TemplatedControl, Fills>("Fill", defaultValue: Fills.Weak);

    /// <summary>
    /// Defines the ShowLabel attached property of the <see cref="StandardButton"/>.
    /// </summary>
    public static readonly AttachedProperty<bool> ShowLabelProperty = AvaloniaProperty.RegisterAttached<StandardButton, TemplatedControl, bool>("ShowLabel", defaultValue: true);

    /// <summary>
    /// Gets or sets the text of the <see cref="StandardButton"/>.
    /// </summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Gets or sets the icon display option of the <see cref="StandardButton"/>.
    /// </summary>
    public ShowIconOptions? ShowIcon
    {
        get => GetValue(ShowIconProperty);
        set => SetValue(ShowIconProperty, value);
    }

    /// <summary>
    /// Gets or sets the left icon of the <see cref="StandardButton"/>.
    /// </summary>
    public IconValue? LeftIcon
    {
        get => GetValue(LeftIconProperty);
        set => SetValue(LeftIconProperty, value);
    }

    /// <summary>
    /// Gets or sets the right icon of the <see cref="StandardButton"/>.
    /// </summary>
    public IconValue? RightIcon
    {
        get => GetValue(RightIconProperty);
        set => SetValue(RightIconProperty, value);
    }
    
    /// <summary>
    /// Gets or sets a value indicating whether the label is shown on the <see cref="StandardButton"/>. Defaults to True.
    /// </summary>
    public bool ShowLabel
    {
        get => GetValue(ShowLabelProperty);
        set => SetValue(ShowLabelProperty, value);
    }
    
    /// <summary>
    /// Gets or sets the type of the <see cref="StandardButton"/>. Defaults to <see cref="Types.Tertiary"/>.
    /// </summary>
    public Types Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    /// <summary>
    /// Gets or sets the size of the <see cref="StandardButton"/>. Defaults to <see cref="Sizes.Medium"/>.
    /// </summary>
    public Sizes Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the fill option of the <see cref="StandardButton"/>. Defaults to <see cref="Fills.Weak"/>.
    /// </summary>
    public Fills Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LeftIconProperty)
        {
            UpdateLeftIcon(change.GetNewValue<IconValue?>());
        }
        else if (change.Property == RightIconProperty)
        {
            UpdateRightIcon(change.GetNewValue<IconValue?>());
        }
        else if (change.Property == TextProperty)
        {
            UpdateLabel(change.GetNewValue<string?>());
        } 
        else if (change.Property == ShowIconProperty)
        {
            UpdateIconVisibility();
        } 
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _content = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
        _border = e.NameScope.Find<Border>("PART_Border");

        if (_content == null || _border == null) return;
        
        // label text
        _label = e.NameScope.Find<TextBlock>("PART_Label");
        if (_label != null)
            UpdateLabel(Text);
        
        // left icon
        _leftIcon = e.NameScope.Find<UnifiedIcon>("PART_LeftIcon");
        if (_leftIcon != null)
            UpdateLeftIcon(LeftIcon);
        
        // right icon
        _rightIcon = e.NameScope.Find<UnifiedIcon>("PART_RightIcon");
        if (_rightIcon != null)
            UpdateRightIcon(RightIcon);
        
        // if Content is not null, display the Content just like a regular button would (using ContentPresenter).
        // Otherwise, build the button from the set properties
        _content.IsVisible = Content is not null;
        _border.IsVisible = Content is null;
        
        base.OnApplyTemplate(e);
    }
    
    /// <summary>
    /// Updates the icon visibility of the <see cref="StandardButton"/>.
    /// </summary>
    private void UpdateIconVisibility()
    {
        if (_leftIcon != null)
            _leftIcon.IsVisible = ShowIcon is ShowIconOptions.Left or ShowIconOptions.Both or ShowIconOptions.IconOnly;
        
        if (_rightIcon != null)
            _rightIcon.IsVisible = ShowIcon is ShowIconOptions.Right or ShowIconOptions.Both;
    }
    
    /// <summary>
    /// Updates the left icon of the <see cref="StandardButton"/>.
    /// </summary>
    /// <param name="newIcon">The new icon value</param>
    private void UpdateLeftIcon(IconValue? newIcon)
    {
        if (_leftIcon == null) return;
        
        _leftIcon.Value = newIcon;
        _leftIcon.IsVisible = ShowIcon is ShowIconOptions.Left or ShowIconOptions.Both or ShowIconOptions.IconOnly;
    }
    
    /// <summary>
    /// Updates the right icon of the <see cref="StandardButton"/>.
    /// </summary>
    /// <param name="newIcon">The new icon value</param>
    private void UpdateRightIcon(IconValue? newIcon)
    {
        if (_rightIcon == null) return;
        
        _rightIcon.Value = newIcon;
        _rightIcon.IsVisible = ShowIcon is ShowIconOptions.Right or ShowIconOptions.Both;
    }
    
    /// <summary>
    /// Updates the label text of the <see cref="StandardButton"/>.
    /// </summary>
    /// <param name="newLabel">The new text value</param>
    private void UpdateLabel(string? newLabel)
    {
        if (_label == null) return;
        
        _label.Text = newLabel;
        _label.IsVisible = ShowLabel && ShowIcon != ShowIconOptions.IconOnly;
    }
}
