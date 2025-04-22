namespace NexusMods.App.UI.MessageBox.Enums;

/// <summary>
/// Specifies the type of window to be used for a message box.
/// </summary>
public enum MessageBoxWindowType
{
    /// <summary>
    /// Represents a modal window, which requires the user to interact with it
    /// before they can return to using other windows in the application.
    /// </summary>
    Modal,

    /// <summary>
    /// Represents a modeless window, which allows the user to interact with other
    /// windows in the application while the message box is open.
    /// </summary>
    Modeless,

    /// <summary>
    /// (Not implemented yet) Represents an embedded window, which is displayed as part of 
    /// the main window rather than as a standalone window.
    /// </summary>
    Embedded
}
