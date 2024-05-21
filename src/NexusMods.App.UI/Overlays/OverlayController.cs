using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NexusMods.App.UI.Overlays;

/// <inheritdoc />
public class OverlayController : IOverlayController, INotifyPropertyChanged
{
    private object _lockObject = new();
    public IOverlayViewModel? CurrentOverlay { get; set; } = null;
    
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

    public async Task<TResult?> Enqueue<TResult>(IOverlayViewModel<TResult> overlayViewModel)
    {
        Enqueue((IOverlayViewModel)overlayViewModel);
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
        OnPropertyChanged(nameof(CurrentOverlay));
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
                OnPropertyChanged(nameof(CurrentOverlay));
                ProcessNext();
            }
            else
            {
                _queue = new Queue<IOverlayViewModel>(_queue.Where(ovm => ovm != model));
            }
        }
    }

#region INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    
    #endregion

}
