using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using R3;

namespace NexusMods.Abstractions.UI;

[PublicAPI]
public interface IReactiveR3Object : ReactiveUI.IReactiveObject, IDisposable
{
    Observable<bool> Activation { get; }
    IDisposable Activate();
    void Deactivate();
}

/// <summary>
/// Base class using R3 with support for activation/deactivation,
/// <see cref="INotifyPropertyChanged"/>, and <see cref="INotifyPropertyChanging"/>.
/// </summary>
[PublicAPI]
public class ReactiveR3Object : IReactiveR3Object
{
    private readonly BehaviorSubject<bool> _activation = new(initialValue: false);
    public Observable<bool> Activation => _activation;

    public IDisposable Activate()
    {
        _activation.OnNext(true);
        return Disposable.Create(this, static self => self.Deactivate());
    }

    public void Deactivate()
    {
        // NOTE(erri120): no need to deactivate disposed objects, as
        // any subscriptions and WhenActivated-blocks are already disposed
        if (_isDisposed) return;
        _activation.OnNext(false);
    }

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

    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangingEventHandler? PropertyChanging;
    public void RaisePropertyChanging(PropertyChangingEventArgs args)
    {
        var propertyChanging = PropertyChanging;
        propertyChanging?.Invoke(this, args);
    }

    public void RaisePropertyChanged(PropertyChangedEventArgs args)
    {
        var propertyChanged = PropertyChanged;
        propertyChanged?.Invoke(this, args);
    }

    protected void RaiseAndSetIfChanged<T>(ref T backingField, T newValue, [CallerMemberName] string? propertyName = null)
    {
        ReactiveUI.IReactiveObjectExtensions.RaiseAndSetIfChanged(this, ref backingField, newValue, propertyName);
    }
}
