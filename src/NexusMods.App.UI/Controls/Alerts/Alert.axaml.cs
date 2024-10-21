using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using NexusMods.Icons;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Interactivity;

namespace NexusMods.App.UI.Controls.Alerts;

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
    
    public static readonly StyledProperty<ReactiveCommand<Unit, AlertSettingsWrapper>> DismissCommandProperty = AvaloniaProperty.Register<Alert, ReactiveCommand<Unit, AlertSettingsWrapper>>(nameof(DismissCommand));

    public static readonly StyledProperty<AlertSettingsWrapper?> AlertSettingsProperty = AvaloniaProperty.Register<Alert, AlertSettingsWrapper?>(nameof(AlertSettings));
    
    public static readonly AttachedProperty<bool> ShowDismissProperty = 
        AvaloniaProperty.RegisterAttached<Alert, TemplatedControl, bool>("ShowDismiss", defaultValue: true);
    
    public static readonly AttachedProperty<bool> ShowBodyProperty = 
        AvaloniaProperty.RegisterAttached<Alert, TemplatedControl, bool>("ShowBody", defaultValue: true);
    
    public static readonly AttachedProperty<bool> ShowActionsProperty = 
        AvaloniaProperty.RegisterAttached<Alert, TemplatedControl, bool>("ShowActions", defaultValue: true);
    
    public static readonly AttachedProperty<bool> IsDismissedProperty = 
        AvaloniaProperty.RegisterAttached<Alert, TemplatedControl, bool>("IsDismissed", defaultValue: false);
    
    public static readonly AttachedProperty<SeverityOptions> SeverityProperty = 
        AvaloniaProperty.RegisterAttached<Alert, TemplatedControl, SeverityOptions>("Severity", defaultValue: SeverityOptions.None);

    private UnifiedIcon? _icon  = null;
    private Button? _dismissButton  = null;
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
    
    public bool ShowDismiss
    {
        get => GetValue(ShowDismissProperty);
        set => SetValue(ShowDismissProperty, value);
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
    
    public bool IsDismissed
    {
        get => GetValue(IsDismissedProperty);
        set => SetValue(IsDismissedProperty, value);
    }
    
    public ReactiveCommand<Unit, AlertSettingsWrapper> DismissCommand
    {
        get => GetValue(DismissCommandProperty);
        private set => SetValue(DismissCommandProperty, value);
    }

    public AlertSettingsWrapper? AlertSettings
    {
        get => GetValue(AlertSettingsProperty);
        set => SetValue(AlertSettingsProperty, value);
    }
    
    public Alert()
    {
        // Start out dismissed so it doesn't show up for a split second and hides again
        IsDismissed = false;
        
        // can dismiss if the alert settings are not null
        var canDismiss = this.WhenAnyValue(x => x.AlertSettings).Select(x => x is not null);
        
        // when the dismiss button is clicked, dismiss the alert
        DismissCommand = ReactiveCommand.Create(() =>
        {
            Console.WriteLine($"DismissCommand {AlertSettings?.Key}");
            IsDismissed = true;
            AlertSettings!.DismissAlert();

            return AlertSettings;
        }, canDismiss);
    }
    
    private readonly SerialDisposable _serialDisposable = new();
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == AlertSettingsProperty)
        {
            _serialDisposable.Disposable = null;

            if (change.NewValue is AlertSettingsWrapper alertSettings)
            {
                _serialDisposable.Disposable = alertSettings
                    .WhenAnyValue(x => x.IsDismissed)
                    .Subscribe(isDismissed => IsDismissed = isDismissed);
            }
        }

        base.OnPropertyChanged(change);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        _serialDisposable.Disposable = null;
        base.OnUnloaded(e);
    }

    
    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        
        _icon = e.NameScope.Find<UnifiedIcon>("Icon");
        _dismissButton = e.NameScope.Find<Button>("DismissButton");
        _titleText = e.NameScope.Find<TextBlock>("TitleText");
        _bodyText = e.NameScope.Find<TextBlock>("BodyText");
        _bodyTextBorder = e.NameScope.Find<Border>("BodyTextBorder");
        _actionsRowBorder = e.NameScope.Find<Border>("ActionsRowBorder");
        
        if (_icon == null || _dismissButton == null || _titleText == null || _bodyText == null || _bodyTextBorder == null || _actionsRowBorder == null) 
            return;

        // no content set, so we hide the actions row
        if (Content == null)
        {
            ShowActions = false;
        }
        
        // hide the body if there is no body text
        if (ShowBody && string.IsNullOrEmpty(Body))
        {
            ShowBody = false;
        }

        // turn off elements based on properties
        _dismissButton.IsVisible = ShowDismiss;
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
