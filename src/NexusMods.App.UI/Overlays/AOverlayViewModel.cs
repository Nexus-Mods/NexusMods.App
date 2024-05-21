using System.Diagnostics;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays;

/// <summary>
/// A view model that represents an overlay.
/// </summary>
/// <typeparam name="TInner"></typeparam>
public abstract class AOverlayViewModel<TInner> : AViewModel<TInner>, IOverlayViewModel
where TInner : class, IViewModelInterface
{
    /// <summary>
    /// The owning controller of the overlay.
    /// </summary>
    [Reactive]
    public IOverlayController? Controller { get; set; }
    
    [Reactive]
    public Status Status { get; set; }

    private readonly TaskCompletionSource _taskCompletionSource = new();
    public Task CompletionTask => _taskCompletionSource.Task;

    public void Close()
    {
        Debug.Assert(Controller != null, "Controller != null");
        
        Controller.Remove(this);
        
        if (Status == Status.Closed)
        {
            return;
        }
        
        Status = Status.Closed;
        _taskCompletionSource.SetResult();
    }
}

public abstract class AOverlayViewModel<TInterface, TResult> : AOverlayViewModel<TInterface>, IOverlayViewModel<TResult> 
    where TInterface : class, IViewModelInterface
{
    public TResult? Result { get; set; }
    
    public void Complete(TResult result)
    {
        Result = result;
        Close();
    }
}
