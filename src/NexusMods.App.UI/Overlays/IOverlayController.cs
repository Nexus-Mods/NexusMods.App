namespace NexusMods.App.UI.Overlays;

/// <summary>
/// Interface for managing and triggering various overlays that can happen within the application.
/// </summary>
public interface IOverlayController
{
    /// <summary>
    /// Tells you when to apply the next overlay to be shown to the screen.
    /// </summary>
    IOverlayViewModel? CurrentOverlay { get; }
    
    /// <summary>
    /// Enqueue an overlay to be shown by the UI
    /// </summary>
    /// <param name="overlayViewModel"></param>
    void Enqueue(IOverlayViewModel overlayViewModel);
    
    /// <summary>
    /// Enqueue an overlay to be shown by the UI and wait for the result, or null
    /// if it was cancelled, or not shown, or otherwise has no result.
    /// </summary>
    Task<TResult?> EnqueueAndWait<TResult>(IOverlayViewModel<TResult> overlayViewModel);
    
    /// <summary>
    /// Removes the overlay from the queue, need not be the current overlay.
    /// </summary>
    /// <param name="model"></param>
    void Remove(IOverlayViewModel model);
}
