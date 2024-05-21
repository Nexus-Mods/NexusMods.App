namespace NexusMods.App.UI.Overlays;

/// <summary>
/// The status of the overlay.
/// </summary>
public enum Status
{
    /// <summary>
    /// Never shown.
    /// </summary>
    NotShown,
    /// <summary>
    /// Currently visible.
    /// </summary>
    Visible,
    /// <summary>
    /// Closed or dismissed.
    /// </summary>
    Closed,
}

public interface IOverlayViewModel : IViewModelInterface
{
    /// <summary>
    /// The owning controller of the overlay.
    /// </summary>
    public IOverlayController? Controller { get; set; }
    
    /// <summary>
    /// Current status of the overlay.
    /// </summary>
    public Status Status { get; set; }
    
    /// <summary>
    /// When the overlay is dismissed, this task is completed.
    /// </summary>
    public Task CompletionTask { get; }
    
    /// <summary>
    /// Closes or dismisses the overlay.
    /// </summary>
    public void Close();
}

/// <summary>
/// A typed overlay view model that can return a result.
/// </summary>
/// <typeparam name="TResult"></typeparam>
public interface IOverlayViewModel<TResult> : IOverlayViewModel
{
    /// <summary>
    /// The result of the overlay.
    /// </summary>
    public TResult? Result { get; set; }

    /// <summary>
    /// Sets the result of the overlay and dismisses it.
    /// </summary>
    /// <param name="result"></param>
    public void Complete(TResult result);
}
