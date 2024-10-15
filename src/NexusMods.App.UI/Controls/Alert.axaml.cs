using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using NexusMods.Icons;

namespace NexusMods.App.UI.Controls;

public class Alert : ContentControl
{
    public enum SeverityOptions
    {
        None,
        Info,
        Success,
        Warning,
        Error
    }
    
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<Alert, string?>(nameof(Title), defaultValue: "Default Title");
    public static readonly StyledProperty<string?> BodyProperty = AvaloniaProperty.Register<Alert, string?>(nameof(Body), defaultValue: "Default Body");
    
    public static readonly AttachedProperty<bool> ShowCloseButtonProperty = 
        AvaloniaProperty.RegisterAttached<Alert, TemplatedControl, bool>("ShowCloseButton", defaultValue: true);
    
    public static readonly AttachedProperty<bool> ShowBodyProperty = 
        AvaloniaProperty.RegisterAttached<Alert, TemplatedControl, bool>("ShowBody", defaultValue: true);
    
    public static readonly AttachedProperty<bool> ShowActionsProperty = 
        AvaloniaProperty.RegisterAttached<Alert, TemplatedControl, bool>("ShowActions", defaultValue: true);
    
    public static readonly AttachedProperty<SeverityOptions> SeverityProperty = 
        AvaloniaProperty.RegisterAttached<Alert, TemplatedControl, SeverityOptions>("Severity", defaultValue: SeverityOptions.None);

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
    
    public SeverityOptions Severity
    {
        get => GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
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

        // no content set, so we hide the actions row
        if (Content == null)
        {
            ShowActions = false;
        }
        
        ShowBody = !string.IsNullOrEmpty(Body);

        // turn off elements based on properties
        _closeButton.IsVisible = ShowCloseButton;
        _bodyTextBorder.IsVisible = ShowBody;
        _actionsRowBorder.IsVisible = ShowActions;
        
        // set the text
        _titleText.Text = Title;
        _bodyText.Text = Body;

        _icon.Value = Severity switch
        {
            // set icon based on severity
            SeverityOptions.Info => IconValues.Info,
            SeverityOptions.Success => IconValues.CheckCircleOutline,
            SeverityOptions.Warning => IconValues.WarningAmber,
            SeverityOptions.Error => IconValues.Warning,
            _ => IconValues.Info,
        };
    }
    
}
