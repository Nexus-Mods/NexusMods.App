using R3;
namespace NexusMods.App.UI.Overlays.Generic.MessageBox.Ok;

/// <summary>
/// Interface for a message box model with an 'ok' button.
/// </summary>
public interface IMessageBoxOkViewModel : IOverlayViewModel<Unit>
{
    public string Title { get; set; }
    public string Description { get; set; }
}
