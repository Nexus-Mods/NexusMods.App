namespace NexusMods.App.UI.Dialog.Enums;

/// <summary>
/// Represents the different styling options for buttons in a Dialog.
/// </summary>
public enum ButtonStyling
{
    /// <summary>
    /// No specific styling is applied to the button.
    /// </summary>
    None,
    
    /// <summary>
    /// The button is styled as a primary action, used for a neutral result.
    /// </summary>
    Default,

    /// <summary>
    /// The button is styled as a primary action, used for a neutral result.
    /// </summary>
    Default,
    
    /// <summary>
    /// The button is styled to indicate a premium action.
    /// </summary>
    Premium,

    /// <summary>
    /// The button is styled to indicate an informational action.
    /// </summary>
    Info,

    /// <summary>
    /// The button is styled as a primary action, used for a positive result.
    /// </summary>
    Primary,

    /// <summary>
    /// The button is styled as a primary action, used for a destructive result and typicall for actions that cannot be undone.
    /// </summary>
    Destructive,
}
