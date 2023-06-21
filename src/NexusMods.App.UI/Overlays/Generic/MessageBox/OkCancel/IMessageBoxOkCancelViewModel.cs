namespace NexusMods.App.UI.Overlays.Generic.MessageBox.OkCancel;

/// <summary>
/// Interface for a message box model with an 'ok' and 'cancel' button.
/// </summary>
public interface IMessageBoxOkCancelViewModel
{
    /// <summary>
    /// When the user presses 'ok'
    /// </summary>
    Action Ok { get; }
    
    /// <summary>
    /// When the user presses 'cancel'
    /// </summary>
    Action Cancel { get; }
}
