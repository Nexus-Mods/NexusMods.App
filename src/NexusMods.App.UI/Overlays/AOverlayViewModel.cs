using System.Diagnostics;
using NexusMods.UI.Sdk;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays;

/// <summary>
/// A view model that represents an overlay.
/// </summary>
/// <typeparam name="TInner"></typeparam>
public abstract class AOverlayViewModel<TInner> : AViewModel<TInner>, IOverlayViewModel
where TInner : class, IViewModelInterface
{
    private IOverlayController? _controller;

    public AOverlayViewModel()
    {
        // Has to be here, because otherwise Fody breaks the codegen and doesn't make a backing
        // field for Status for some reason :|
        Status = Status.Hidden;
    }

    /// <inheritdoc/>
    public IOverlayController Controller
    {
        get => _controller ?? throw new InvalidOperationException("Controller must be set!");
        set => this.RaiseAndSetIfChanged(ref _controller, value);
    }
    
    [Reactive] 
    public Status Status { get; set; }

    private readonly TaskCompletionSource _taskCompletionSource = new();
    public Task CompletionTask => _taskCompletionSource.Task;

    public virtual void Close()
    {
        Debug.Assert(Controller != null, "Controller != null");
        if (Status == Status.Closed)
        {
            return;
        }
        
        Controller.Remove(this);
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
