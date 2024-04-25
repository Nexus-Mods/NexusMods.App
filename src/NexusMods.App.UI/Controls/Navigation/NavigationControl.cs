using System.Diagnostics;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using JetBrains.Annotations;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Navigation;

[PublicAPI]
public class NavigationControl : Button
{
    public static readonly StyledProperty<ReactiveCommand<NavigationInput, Unit>?> NavigateCommandProperty =
        AvaloniaProperty.Register<NavigationControl, ReactiveCommand<NavigationInput, Unit>?>(nameof(NavigationCommand));

    public ReactiveCommand<NavigationInformation, Unit>? NavigationCommand
    {
        get
        {
            var value = GetValue(CommandProperty);
            return value as ReactiveCommand<NavigationInformation, Unit>;
        }
        set => SetValue(CommandProperty, value);
    }

    private NavigationInformation NavigationInformation
    {
        get => (NavigationInformation)CommandParameter!;
        set => CommandParameter = value;
    }

    private readonly ReactiveCommand<OpenPageBehaviorType, Unit> _contextMenuCommand;

    public NavigationControl()
    {
        _contextMenuCommand = ReactiveCommand.Create<OpenPageBehaviorType>(openPageBehaviorType =>
        {
            NavigationInformation = NavigationInformation.From(openPageBehaviorType);
            OnClick();
        });

        var contextMenu = new ContextMenu
        {
            Items =
            {
                new MenuItem
                {
                    Header = "Open in new tab",
                    Command = _contextMenuCommand,
                    CommandParameter = OpenPageBehaviorType.NewTab,
                },
                new MenuItem
                {
                    Header = "Open in new panel",
                    Command = _contextMenuCommand,
                    CommandParameter = OpenPageBehaviorType.NewPanel,
                },
            },
        };

        ContextMenu = contextMenu;
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(Button);

    private bool _wasOnPointerPressedCalled;

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _wasOnPointerPressedCalled = true;

        var input = ToNavigationInput(e);
        NavigationInformation = NavigationInformation.From(input);

        base.OnPointerPressed(e);

        // NOTE(erri120): Button.OnPointerPressed only calls OnClick if the
        // button used was the left mouse button, but we also want OnClick
        // to trigger on the middle mouse button.
        if (e.Handled) return;
        if (ClickMode == ClickMode.Press) OnClick();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        // NOTE(erri120): Can't get the input in this event, we'll just re-use
        // the value set in OnPointerPressed. This assumes that OnPointerPressed
        // always gets called before OnPointerReleased.
        Debug.Assert(_wasOnPointerPressedCalled);
        _wasOnPointerPressedCalled = false;

        base.OnPointerReleased(e);

        // NOTE(erri120): Button.OnPointerReleased only calls OnClick if the
        // button used was the left mouse button, but we also want OnClick
        // to trigger on the middle mouse button.
        if (e.Handled) return;
        if (ClickMode == ClickMode.Release) OnClick();
    }

    private NavigationInput ToNavigationInput(PointerEventArgs e)
    {
        var keyModifiers = e.KeyModifiers;
        var properties = e.GetCurrentPoint(this).Properties;

        var keyType = properties.IsLeftButtonPressed
            ? MouseButton.Left
            : properties.IsMiddleButtonPressed
                ? MouseButton.Middle
                : MouseButton.Left;

        return new NavigationInput(keyType, keyModifiers);
    }
}
