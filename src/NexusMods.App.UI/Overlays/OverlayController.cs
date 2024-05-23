using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays;

/// <inheritdoc />
public class OverlayController : ReactiveObject, IOverlayController
{
    private readonly object _lockObject = new();
    public IOverlayViewModel? CurrentOverlay { get; set; }
    
    private Queue<IOverlayViewModel> _queue = new();
    public void Enqueue(IOverlayViewModel overlayViewModel)
    {
        lock (_lockObject)
        {
            overlayViewModel.Controller = this;
            _queue.Enqueue(overlayViewModel);
            
            ProcessNext();
        }
    }

    public async Task<TResult?> EnqueueAndWait<TResult>(IOverlayViewModel<TResult> overlayViewModel)
    {
        Enqueue(overlayViewModel);
        await overlayViewModel.CompletionTask;
        return overlayViewModel.Result;
    }

    private void ProcessNext()
    {
        if (CurrentOverlay != null)
        {
            return;
        }
        if (_queue.Count == 0)
        {
            return;
        }
        
        CurrentOverlay = _queue.Dequeue();
        CurrentOverlay.Status = Status.Visible;
        NotifyCurrentOverlayChanged();
    }

    private void NotifyCurrentOverlayChanged()
    {
        Dispatcher.UIThread.Invoke(() =>
            {
                this.RaisePropertyChanged(nameof(CurrentOverlay));
            }
        );
    }

    public void Remove(IOverlayViewModel model)
    {
        lock (_lockObject)
        {
            if (CurrentOverlay == model)
            {
                var oldOverlay = CurrentOverlay;
                oldOverlay.Status = Status.Closed;
                CurrentOverlay = null;
                NotifyCurrentOverlayChanged();
                ProcessNext();
            }
            else
            {
                var items = _queue.ToArray();
                _queue.Clear();
                foreach (var item in items)
                {
                    if (item == model)
                        continue;
                    _queue.Enqueue(item);
                }
            }
        }
    }
}
