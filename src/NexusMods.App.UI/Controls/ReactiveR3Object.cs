using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using R3;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Base class using R3 with support for activation/deactivation,
/// <see cref="INotifyPropertyChanged"/>, and <see cref="INotifyPropertyChanging"/>.
/// </summary>
[PublicAPI]
public class ReactiveR3Object : ReactiveUI.IReactiveObject, IDisposable
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
