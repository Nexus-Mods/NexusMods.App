using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using NexusMods.Icons;

namespace NexusMods.App.UI.Controls.PageHeader;

public class PageHeader : ContentControl
{

    /// <summary>
    /// Defines the Title property of the <see cref="PageHeader"/>.
    /// </summary>
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<PageHeader, string?>(nameof(Title), defaultValue: "Default Title");
    
    /// <summary>
    /// Defines the Title property of the <see cref="PageHeader"/>.
    /// </summary>
    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<PageHeader, string?>(nameof(Description), defaultValue: "This is the default description of the page.");

    /// <summary>
    /// Defines the Title property of the <see cref="PageHeader"/>.
    /// </summary>
    public static readonly StyledProperty<IconValue?> IconProperty = AvaloniaProperty.Register<PageHeader, IconValue?>(nameof(Icon), defaultValue: IconValues.PictogramBox2);
    
    /// <summary>
    /// Gets or sets the title text of the <see cref="PageHeader"/>.
    /// </summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    /// <summary>
    /// Gets or sets the description text of the <see cref="PageHeader"/>.
    /// </summary>
    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
    
    /// <summary>
    /// Gets or sets the icon of the <see cref="PageHeader"/>.
    /// </summary>
    public IconValue? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
    
    private TextBlock? _titleText;
    private TextBlock? _descriptionText;
    private UnifiedIcon? _unifiedIcon;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == TitleProperty)
        {
            UpdateTitle(change.GetNewValue<string?>());
        } 
        else if (change.Property == DescriptionProperty)
        {
            UpdateDescription(change.GetNewValue<string?>());
        }
        else if (change.Property == IconProperty)
        {
            UpdateIcon(change.GetNewValue<IconValue?>());
        }
    }
    
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _titleText = e.NameScope.Find<TextBlock>("TitleTextBlock");
        if (_titleText != null)
            UpdateTitle(Title);
        
        _descriptionText = e.NameScope.Find<TextBlock>("DescriptionTextBlock");
        if (_descriptionText != null)
            UpdateDescription(Description);
        
        _unifiedIcon = e.NameScope.Find<UnifiedIcon>("Icon");
        if (_unifiedIcon != null)
            UpdateIcon(Icon);
    }


    /// <summary>
    /// Updates the visual title text of the <see cref="PageHeader"/>.
    /// </summary>
    /// <param name="newTitle">The new title text</param>
    private void UpdateTitle(string? newTitle)
    {
        if (_titleText != null)
            _titleText.Text = newTitle;
    }
    
    /// <summary>
    /// Updates the visual title text of the <see cref="PageHeader"/>.
    /// </summary>
    /// <param name="newDescription">The new description text</param>
    private void UpdateDescription(string? newDescription)
    {
        if (_descriptionText != null)
            _descriptionText.Text = newDescription;
    }    
    
    /// <summary>
    /// Updates the visual icon of the <see cref="PageHeader"/>.
    /// </summary>
    /// <param name="newIcon">The new icon</param>
    private void UpdateIcon(IconValue? newIcon)
    {
        if (_unifiedIcon != null)
            _unifiedIcon.Value = newIcon;
    }
}
