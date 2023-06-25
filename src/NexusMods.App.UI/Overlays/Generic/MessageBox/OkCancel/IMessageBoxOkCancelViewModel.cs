namespace NexusMods.App.UI.Overlays.Generic.MessageBox.OkCancel;

/// <summary>
/// Interface for a message box model with an 'ok' and 'cancel' button.
/// </summary>
public interface IMessageBoxOkCancelViewModel : IOverlayViewModel
{
    /// <summary>
    /// When the user presses 'ok'
    /// </summary>
    Action Ok { get; }
}
