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
        Error,
    }

    /// <summary>
    /// Defines the Title property of the <see cref="Alert"/>.
    /// </summary>
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<Alert, string?>(nameof(Title), defaultValue: "");

    /// <summary>
    /// Defines the Body property of the <see cref="Alert"/>..
    /// </summary>
    public static readonly StyledProperty<string?> BodyProperty = AvaloniaProperty.Register<Alert, string?>(nameof(Body), defaultValue: "");


    /// <summary>
    /// Defines the DismissCommand attached property of the <see cref="Alert"/>.
    /// </summary>
    public static readonly AttachedProperty<ReactiveCommand<Unit, AlertSettingsWrapper>> DismissCommandProperty =
        AvaloniaProperty.RegisterAttached<Alert, TemplatedControl, ReactiveCommand<Unit, AlertSettingsWrapper>>(nameof(DismissCommand));

    /// <summary>
    /// Defines the AlertSettings attached property of the <see cref="Alert"/>.
    /// </summary>
    public static readonly AttachedProperty<AlertSettingsWrapper?> AlertSettingsProperty = AvaloniaProperty.RegisterAttached<Alert, TemplatedControl, AlertSettingsWrapper?>(nameof(AlertSettings));

    /// <summary>
    /// Defines the ShowDismiss attached property of the <see cref="Alert"/>. Defaults to True.
    /// </summary>
    public static readonly AttachedProperty<bool> ShowDismissProperty =
        AvaloniaProperty.RegisterAttached<Alert, TemplatedControl, bool>("ShowDismiss", defaultValue: true);

    /// <summary>
    /// Defines the ShowBody attached property of the <see cref="Alert"/>. Defaults to True.
    /// </summary>
    public static readonly AttachedProperty<bool> ShowBodyProperty =
        AvaloniaProperty.RegisterAttached<Alert, TemplatedControl, bool>("ShowBody", defaultValue: true);

    /// <summary>
    /// Defines the ShowActions attached property of the <see cref="Alert"/>. Defaults to True.
    /// </summary>
    public static readonly AttachedProperty<bool> ShowActionsProperty =
        AvaloniaProperty.RegisterAttached<Alert, TemplatedControl, bool>("ShowActions", defaultValue: true);

    /// <summary>
    /// Defines the IsDismissed attached property of the <see cref="Alert"/>. Defaults to False.
    /// </summary>
    public static readonly AttachedProperty<bool> IsDismissedProperty =
        AvaloniaProperty.RegisterAttached<Alert, TemplatedControl, bool>("IsDismissed", defaultValue: false);

    /// <summary>
    /// Defines the Severity attached property of the <see cref="Alert"/>. Defaults to <see cref="SeverityOptions.None"/>.
    /// </summary>
    public static readonly AttachedProperty<SeverityOptions> SeverityProperty =
        AvaloniaProperty.RegisterAttached<Alert, TemplatedControl, SeverityOptions>("Severity", defaultValue: SeverityOptions.None);

    private UnifiedIcon? _icon = null;
    private Button? _dismissButton = null;
    private TextBlock? _titleText = null;
    private TextBlock? _bodyText = null;
    private Border? _bodyTextBorder = null;
    private Border? _actionsRowBorder = null;

    /// <summary>
    /// Gets or sets the title text of the <see cref="Alert"/>.
    /// </summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Gets or sets the body text of the <see cref="Alert"/>.
    /// </summary>
    public string? Body
    {
        get => GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Dismiss button is shown on the <see cref="Alert"/>.
    /// </summary>
    public bool ShowDismiss
    {
        get => GetValue(ShowDismissProperty);
        set => SetValue(ShowDismissProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Body text is shown on the <see cref="Alert"/>.
    /// </summary>
    public bool ShowBody
    {
        get => GetValue(ShowBodyProperty);
        set => SetValue(ShowBodyProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether Actions are shown on the <see cref="Alert"/>.
    /// </summary>
    public bool ShowActions
    {
        get => GetValue(ShowActionsProperty);
        set => SetValue(ShowActionsProperty, value);
    }

    /// <summary>
    /// Gets or sets the Severity option of the <see cref="Alert"/>.
    /// </summary>
    public SeverityOptions Severity
    {
        get => GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }

    /// <summary>
    /// Gets the dismissed value of the <see cref="Alert"/>.
    /// </summary>
    public bool IsDismissed
    {
        get => GetValue(IsDismissedProperty);
        private set => SetValue(IsDismissedProperty, value);
    }

    /// <summary>
    /// Gets the Dismiss command of the <see cref="Alert"/>.
    /// </summary>
    public ReactiveCommand<Unit, AlertSettingsWrapper> DismissCommand
    {
        get => GetValue(DismissCommandProperty);
        private set => SetValue(DismissCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the AlertSettings of the <see cref="Alert"/>.
    /// </summary>
    public AlertSettingsWrapper? AlertSettings
    {
        get => GetValue(AlertSettingsProperty);
        set => SetValue(AlertSettingsProperty, value);
    }

    public Alert()
    {
        IsDismissed = false;

        // Create an observable that determines if the alert can be dismissed
        var canDismiss = this.WhenAnyValue(x => x.AlertSettings).Select(x => x is not null);

        // Create the DismissCommand which dismisses the alert when executed
        // The command can only be executed if canDismiss is true
        DismissCommand = ReactiveCommand.Create(() =>
            {
                IsDismissed = true;

                // Call the DismissAlert method on the AlertSettings object
                AlertSettings!.DismissAlert();

                // Return the AlertSettings object
                return AlertSettings;
            }, canDismiss
        );
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


        // turn off elements based on properties
        _dismissButton.IsVisible = ShowDismiss;
        _bodyTextBorder.IsVisible = ShowBody && !string.IsNullOrWhiteSpace(Body);
        _actionsRowBorder.IsVisible = Content != null && ShowActions;

        // set the text
        _titleText.Text = Title;
        _bodyText.Text = Body;

        // set icon based on severity
        _icon.Value = Severity switch
        {
            SeverityOptions.Info => IconValues.Info,
            SeverityOptions.Success => IconValues.CheckCircleOutline,
            SeverityOptions.Warning => IconValues.WarningAmber,
            SeverityOptions.Error => IconValues.Warning,
            _ => IconValues.Info,
        };
    }
}
