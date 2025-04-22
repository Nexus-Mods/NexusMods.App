using System;

namespace NexusMods.App.UI.MessageBox.Enums;

/// <summary>
/// Represents the roles that a button can have in a message box dialog.
/// </summary>
[Flags]
public enum ButtonRole
{
    /// <summary>
    /// No specific role is assigned to the button.
    /// </summary>
    None = 0,

    /// <summary>
    /// The button causes the dialog to be accepted (e.g., "OK").
    /// </summary>
    AcceptRole = 1 << 0,

    /// <summary>
    /// The button causes the dialog to be rejected (e.g., "Cancel").
    /// </summary>
    RejectRole = 1 << 1,

    /// <summary>
    /// The button performs a destructive action (e.g., "Discard Changes") and closes the dialog.
    /// </summary>
    DestructiveRole = 1 << 2,

    /// <summary>
    /// The button performs an action that modifies elements within the dialog.
    /// </summary>
    ActionRole = 1 << 3,

    /// <summary>
    /// The button provides access to help or additional information.
    /// </summary>
    HelpRole = 1 << 4,

    /// <summary>
    /// The button represents a "Yes"-like action.
    /// </summary>
    YesRole = 1 << 5,

    /// <summary>
    /// The button represents a "No"-like action.
    /// </summary>
    NoRole = 1 << 6,

    /// <summary>
    /// The button resets the dialog's fields to their default values.
    /// </summary>
    ResetRole = 1 << 7,

    /// <summary>
    /// The button applies the current changes without closing the dialog.
    /// </summary>
    ApplyRole = 1 << 8,

    /// <summary>
    /// The button performs a premium action.
    /// </summary>
    PremiumRole = 1 << 9,

    /// <summary>
    /// The button provides informational feedback or details.
    /// </summary>
    InfoRole = 1 << 10,

    /// <summary>
    /// The button is marked as a primary action in the dialog.
    /// </summary>
    Primary = 1 << 11,
}
