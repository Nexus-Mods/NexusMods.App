using Avalonia.Controls.Notifications;
using NexusMods.UI.Sdk.Dialog;

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
    /// <param name="buttonDefinitions">Array of buttons to be added to the toast (no need to pass in the Close button)</param>
    /// <param name="buttonHandler">Callback for handling button clicks, should take the ButtonDefinitionId of the clicked button</param>
    /// <returns></returns>
    public bool Show(
        string message,
        ToastNotificationVariant type = ToastNotificationVariant.Neutral,
        TimeSpan? expiration = null,
        DialogButtonDefinition[]? buttonDefinitions = null,
        Action<ButtonDefinitionId>? buttonHandler = null);

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
