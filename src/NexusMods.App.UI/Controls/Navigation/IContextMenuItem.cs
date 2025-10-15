using System.Reactive;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Navigation;

/// <summary>
/// Represents a context menu item for navigation controls.
/// </summary>
/// <remarks>
/// Provides a standardised way to extend <see cref="NavigationControl"/> context menus
/// with custom actions like collection deletion.
/// </remarks>
public interface IContextMenuItem
{
    /// <summary>
    /// Display text shown to the user in the context menu.
    /// </summary>
    string Header { get; }
    
    /// <summary>
    /// Optional icon displayed alongside the text.
    /// </summary>
    IconValue? Icon { get; }
    
    /// <summary>
    /// Command executed when the menu item is clicked.
    /// </summary>
    ReactiveCommand<Unit, Unit> Command { get; }
    
    /// <summary>
    /// Whether the menu item is visible to the user.
    /// </summary>
    bool IsVisible { get; }
    
    /// <summary>
    /// Styling applied to the menu item.
    /// </summary>
    ContextMenuItemStyling Styling { get; }

}
