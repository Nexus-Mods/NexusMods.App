using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Navigation;

[PublicAPI]
public class NavigationControl : Button
{
    public static readonly StyledProperty<ReactiveCommand<NavigationInput, Unit>?> NavigateCommandProperty =
        AvaloniaProperty.Register<NavigationControl, ReactiveCommand<NavigationInput, Unit>?>(nameof(NavigationCommand));

    public ReactiveCommand<NavigationInput, Unit>? NavigationCommand
    {
        get
        {
            var value = GetValue(CommandProperty);
            return value as ReactiveCommand<NavigationInput, Unit>;
        }
        set => SetValue(CommandProperty, value);
    }

    private Optional<NavigationInput> NavigationInput
    {
        set => CommandParameter = value;
    }

    public NavigationControl()
    {
        // TODO: design this context menu and decide what goes in it
        // NOTE: we'll also want consumers to disable/enable certain items
        // TODO: implement click handlers for these items
        var contextMenu = new ContextMenu
        {
            Items =
            {
                new MenuItem
                {
                    Header = "Open in new tab",
                },
                new MenuItem
                {
                    Header = "Open in new panel",
                },
                new MenuItem
                {
                    Header = "Open in new window",
                },
            },
        };

        ContextMenu = contextMenu;
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(Button);

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var keyModifiers = e.KeyModifiers;
        var properties = e.GetCurrentPoint(this).Properties;

        var keyType = properties.IsLeftButtonPressed
            ? MouseButton.Left
            : properties.IsMiddleButtonPressed
                ? MouseButton.Middle
                : MouseButton.None;

        NavigationInput = new NavigationInput(keyType, keyModifiers);
        base.OnPointerPressed(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsPressedProperty)
        {
            if (!change.GetNewValue<bool>())
            {
                NavigationInput = Optional<NavigationInput>.None;
            }
        }
    }
}
