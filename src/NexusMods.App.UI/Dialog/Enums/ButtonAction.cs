namespace NexusMods.App.UI.Dialog.Enums;

/// <summary>
/// Represents the action that a button can have in a dialog.
/// </summary>
public enum ButtonAction
{
    /// <summary>
    /// No specific action is assigned to the button.
    /// </summary>
    None,

    /// <summary>
    /// This button causes the dialog to be accepted when pressing Enter (e.g. "OK").
    /// </summary>
    Accept,

    /// <summary>
    /// This button causes the dialog to be rejected when pressing Esc (e.g., "Cancel").
    /// </summary>
    Reject,
}
