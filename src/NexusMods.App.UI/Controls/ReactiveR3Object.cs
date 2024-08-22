using R3;

namespace NexusMods.App.UI.Controls;

public class ReactiveR3Object : ReactiveUI.ReactiveObject, IDisposable
{
    private readonly BehaviorSubject<bool> _activation = new(initialValue: false);
    public Observable<bool> Activation => _activation;

    internal void Activate() => _activation.OnNext(true);
    internal void Deactivate() => _activation.OnNext(false);

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private bool _isDisposed;

    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            _activation.Dispose();
        }

        _isDisposed = true;
    }
}
