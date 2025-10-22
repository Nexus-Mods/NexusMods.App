namespace NexusMods.App.UI.Controls.Navigation;

/// <summary>
/// Represents the different styling options for context menu items.
/// </summary>
public enum ContextMenuItemStyling
{
    /// <summary>
    /// No specific styling is applied to the menu item.
    /// </summary>
    None,
    
    /// <summary>
    /// The menu item is styled as a default action, used for a neutral result.
    /// </summary>
    Default,
    
    /// <summary>
    /// The menu item is styled to indicate a critical/destructive action.
    /// </summary>
    Critical,

    /// <summary>
    /// The menu item is styled to indicate a premium action.
    /// </summary>
    Premium,
}