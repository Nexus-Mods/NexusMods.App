using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
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
        /// Weak fill.
        /// </summary>
        Weak,
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

        _label.IsVisible = ShowLabel && ShowIcon != ShowIconOptions.IconOnly;

        // if Content is not null, display the Content just like a regular button would (using ContentPresenter).
        // Otherwise, build the button from the set properties
        _content.IsVisible = Content is not null;
        _border.IsVisible = Content is null;
        
        _leftIcon.IsVisible = ShowIcon is ShowIconOptions.Left or ShowIconOptions.Both or ShowIconOptions.IconOnly;
        _rightIcon.IsVisible = ShowIcon is ShowIconOptions.Right or ShowIconOptions.Both;
    }
}
