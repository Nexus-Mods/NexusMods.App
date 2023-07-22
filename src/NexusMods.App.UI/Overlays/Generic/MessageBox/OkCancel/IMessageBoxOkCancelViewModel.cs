namespace NexusMods.App.UI.Overlays.Generic.MessageBox.OkCancel;

/// <summary>
/// Interface for a message box model with an 'ok' and 'cancel' button.
/// </summary>
public interface IMessageBoxOkCancelViewModel : IOverlayViewModel
{
    /// <summary>
    /// True if the user clicked 'ok', false if the user clicked 'cancel' or closed dialog.
    /// </summary>
    bool DialogResult { get; set; }
}
