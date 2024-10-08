using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using NexusMods.Icons;

namespace NexusMods.App.UI.Controls;

public class Alert : TemplatedControl
{
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<Alert, string?>(nameof(Title), defaultValue: "Default Title");
    public static readonly StyledProperty<string?> BodyProperty = AvaloniaProperty.Register<Alert, string?>(nameof(Body), defaultValue: "Default Body");
    
    public static readonly AttachedProperty<bool> ShowCloseButtonProperty = 
        AvaloniaProperty.RegisterAttached<Alert, TemplatedControl, bool>("ShowCloseButton", defaultValue: true);
    
    public static readonly AttachedProperty<bool> ShowBodyProperty = 
        AvaloniaProperty.RegisterAttached<Alert, TemplatedControl, bool>("ShowBody", defaultValue: true);
    
    public static readonly AttachedProperty<bool> ShowActionsProperty = 
        AvaloniaProperty.RegisterAttached<Alert, TemplatedControl, bool>("ShowActions", defaultValue: true);

    private UnifiedIcon? _icon  = null;
    private Button? _closeButton  = null;
    private TextBlock? _titleText  = null;
    private TextBlock? _bodyText  = null;
    private Border? _bodyTextBorder  = null;
    private Border? _actionsRowBorder  = null;
    

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    
    public string? Body
    {
        get => GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }
    
    public bool ShowCloseButton
    {
        get => GetValue(ShowCloseButtonProperty);
        set => SetValue(ShowCloseButtonProperty, value);
    }
    
    public bool ShowBody
    {
        get => GetValue(ShowBodyProperty);
        set => SetValue(ShowBodyProperty, value);
    }
    
    public bool ShowActions
    {
        get => GetValue(ShowActionsProperty);
        set => SetValue(ShowActionsProperty, value);
    }
    
    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        
        _icon = e.NameScope.Find<UnifiedIcon>("Icon");
        _closeButton = e.NameScope.Find<Button>("CloseButton");
        _titleText = e.NameScope.Find<TextBlock>("TitleText");
        _bodyText = e.NameScope.Find<TextBlock>("BodyText");
        _bodyTextBorder = e.NameScope.Find<Border>("BodyTextBorder");
        _actionsRowBorder = e.NameScope.Find<Border>("ActionsRowBorder");
        
        if (_icon == null || _closeButton == null || _titleText == null || _bodyText == null || _bodyTextBorder == null || _actionsRowBorder == null) 
            return;

        // turn off elements based on properties
        _closeButton.IsVisible = ShowCloseButton;
        _bodyTextBorder.IsVisible = ShowBody;
        _actionsRowBorder.IsVisible = ShowActions;
        
        // set the text
        _titleText.Text = Title;
        _bodyText.Text = Body;
        
        // set icon based on class
        if (Classes.Contains("Info"))
        {
            _icon.Value = IconValues.Info;
            
        } else if (Classes.Contains("Success"))
        {
            _icon.Value = IconValues.CheckCircleOutline;
            
        } else if (Classes.Contains("Warning"))
        {
            _icon.Value = IconValues.WarningAmber;
            
        } else if (Classes.Contains("Error"))
        {
            _icon.Value = IconValues.Warning;
        }
        else
        {
            _icon.Value = IconValues.Info;
        }
    }
    
}
