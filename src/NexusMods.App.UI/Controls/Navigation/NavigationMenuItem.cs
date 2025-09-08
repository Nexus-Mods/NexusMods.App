using System.Reactive;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using DynamicData.Kernel;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Navigation;

public class NavigationMenuItem : MenuItem
{
    public static readonly StyledProperty<ReactiveCommand<NavigationInput, Unit>?> NavigateCommandProperty =
        AvaloniaProperty.Register<NavigationControl, ReactiveCommand<NavigationInput, Unit>?>(nameof(NavigationCommand));

    public ICommand? NavigationCommand
    {
        get
        {
            var value = GetValue(CommandProperty);
            return value;
        }
        set => SetValue(CommandProperty, value);
    }

    private NavigationInformation NavigationInformation
    {
        get => (NavigationInformation)CommandParameter!;
        set => CommandParameter = value;
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(MenuItem);

    private readonly ReactiveCommand<OpenPageBehaviorType, Unit> _contextMenuCommand;

    public NavigationMenuItem()
    {
        NavigationInformation = new NavigationInformation(NavigationInput.Default, Optional<OpenPageBehaviorType>.None);

        _contextMenuCommand = ReactiveCommand.Create<OpenPageBehaviorType>(openPageBehaviorType =>
        {
            var navigationInformation = NavigationInformation.From(openPageBehaviorType);
            NavigationCommand?.Execute(navigationInformation);
        });

        var contextMenu = new ContextMenu
        {
            Items =
            {
                new MenuItem
                {
                    Header = Language.NavigationControl_NavigationControl_Open_in_new_tab,
                    Command = _contextMenuCommand,
                    CommandParameter = OpenPageBehaviorType.NewTab,
                },
                new MenuItem
                {
                    Header = Language.NavigationControl_NavigationControl_Open_in_new_panel,
                    Command = _contextMenuCommand,
                    CommandParameter = OpenPageBehaviorType.NewPanel,
                },
            },
        };

        ContextMenu = contextMenu;
    }
}
