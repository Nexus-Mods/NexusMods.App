using Avalonia.Controls.Notifications;

namespace NexusMods.UI.Sdk;

/// <summary>
/// Service for issuing UI toast-like notifications
/// </summary>
public interface IWindowNotificationService
{
    
    /// <summary>
    /// Shows a toast-like notification in the UI.
    /// </summary>
    /// <param name="message">Content of the notification</param>
    /// <param name="type">the type of the notification</param>
    /// <param name="expiration">
    ///   the expiration time of the notification after which it will automatically close.
    ///   If the value is Zero then the notification will remain open until the user closes it.
    ///   Defaults to 5 seconds
    /// </param>
    /// <param name="onClick">an Action to be run when the notification is clicked, defaults to dismiss</param>
    /// <param name="onClose">an Action to be run when the notification is closed</param>
    /// <returns>False if unable to access window object to display notification to, true otherwise</returns>
    bool Show(string message, ToastNotificationVariant type, TimeSpan? expiration = null, Action? onClick = null, Action? onClose = null);
    
}

/// <summary>
/// Types of toast notifications
/// </summary>
public enum ToastNotificationVariant
{
    /// <summary>
    /// Used for informative updates that don’t indicate success or failure.
    /// Examples: “Download started”, “External changes detected”.
    /// Actions: May include an optional “View” or “Open” action.
    /// </summary>
    Neutral,
    
    /// <summary>
    /// Used when an operation completes successfully.
    /// Examples: “Download complete”, “Collection installed”.
    /// Actions: May include an optional “View” or “Open” action.
    /// </summary>
    Success,
    
    /// <summary>
    /// Used when an operation fails or cannot proceed.
    /// Examples: “Download failed”, “Insufficient disk space”.
    /// Actions: Where possible, include a direct recovery option like “Retry” or “Fix now”.
    /// </summary>
    Failure,
}
