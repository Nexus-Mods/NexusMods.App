using System.Diagnostics;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using JetBrains.Annotations;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Navigation;

[PublicAPI]
public class NavigationControl : StandardButton
{
    public static readonly StyledProperty<ReactiveCommand<NavigationInput, Unit>?> NavigateCommandProperty =
        AvaloniaProperty.Register<NavigationControl, ReactiveCommand<NavigationInput, Unit>?>(nameof(NavigationCommand));

    public static readonly StyledProperty<IReadOnlyList<IContextMenuItem>?> AdditionalContextMenuItemsProperty =
        AvaloniaProperty.Register<NavigationControl, IReadOnlyList<IContextMenuItem>?>(
            nameof(AdditionalContextMenuItems));

    public ReactiveCommand<NavigationInformation, Unit>? NavigationCommand
    {
        get
        {
            var value = GetValue(CommandProperty);
            return value as ReactiveCommand<NavigationInformation, Unit>;
        }
        set => SetValue(CommandProperty, value);
    }

    public IReadOnlyList<IContextMenuItem>? AdditionalContextMenuItems
    {
        get => GetValue(AdditionalContextMenuItemsProperty);
        set => SetValue(AdditionalContextMenuItemsProperty, value);
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

        this.WhenAnyValue(x => x.AdditionalContextMenuItems)
            .Subscribe(_ => UpdateContextMenu());

        UpdateContextMenu();
    }

    private void UpdateContextMenu()
    {
        var contextMenu = new ContextMenu();

        // Add standard items
        contextMenu.Items.Add(new MenuItem
        {
            Header = Language.NavigationControl_NavigationControl_Open_in_new_tab,
            Command = _contextMenuCommand,
            CommandParameter = OpenPageBehaviorType.NewTab,
        });

        contextMenu.Items.Add(new MenuItem
        {
            Header = Language.NavigationControl_NavigationControl_Open_in_new_panel,
            Command = _contextMenuCommand,
            CommandParameter = OpenPageBehaviorType.NewPanel,
        });

        // Add additional items if any exist
        var additionalItems = AdditionalContextMenuItems?
            .Where(item => item.IsVisible && item.IsEnabled)
            .ToList();

        if (additionalItems?.Count > 0)
        {
            contextMenu.Items.Add(new Separator());

            foreach (var item in additionalItems)
            {
                var menuItem = new MenuItem
                {
                    Header = item.Header,
                    Command = item.Command,
                };
                
                if (item.Icon != null)
                {
                    menuItem.Icon = new UnifiedIcon 
                    { 
                        Value = item.Icon,
                        Size = 16,
                    };
                }
                contextMenu.Items.Add(menuItem);
            }
        }

        ContextMenu = contextMenu;
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(StandardButton);

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
        // the value set in OnPointerPressed.
        // NOTE(Al12rs): It's somehow possible that OnPointerPressed was not called before OnPointerReleased.
        // Happens very rarely, not sure how or why, but we should just ignore it if it happens.
        Debug.Assert(_wasOnPointerPressedCalled);
        if (!_wasOnPointerPressedCalled) return;
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
