namespace NexusMods.App.UI.Overlays;

public interface IOverlayViewModel : IViewModelInterface
{
    /// <summary>
    /// When this signals 'false', the overlay is dismissed.
    /// </summary>
    public bool IsActive { get; set; }
}
