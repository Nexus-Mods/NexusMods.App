using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using R3;
using ReactiveUI;

namespace NexusMods.Abstractions.UI;

[PublicAPI]
public interface IReactiveR3Object : ReactiveUI.IReactiveObject, IDisposable
{
    Observable<bool> Activation { get; }
    bool IsActivated { get; }
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
    /// <inheritdoc />
    public Observable<bool> Activation => _activation;
    /// <inheritdoc />
    public bool IsActivated => _activation.Value;

    /// <inheritdoc />
    public IDisposable Activate()
    {
        _activation.OnNext(true);
        return Disposable.Create(this, static self => self.Deactivate());
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;
    /// <inheritdoc />
    public event PropertyChangingEventHandler? PropertyChanging;

    /// <inheritdoc />
    public void RaisePropertyChanging(PropertyChangingEventArgs args) => PropertyChanging?.Invoke(this, args);

    /// <inheritdoc />
    public void RaisePropertyChanged(PropertyChangedEventArgs args) => PropertyChanged?.Invoke(this, args);

    /// <summary>
    /// Updates the backing field of a property, raising its <see cref="PropertyChanging"/>
    /// and <see cref="PropertyChanged"/> event if the new value is different from the old value.
    /// </summary>
    /// <param name="backingField">The field to set.</param>
    /// <param name="newValue">The new value of the field.</param>
    /// <param name="propertyName">
    ///     The Name of the property which changed.
    ///     This is auto set via <see cref="CallerMemberNameAttribute"/> if invoking the property directly.
    /// </param>
    protected void RaiseAndSetIfChanged<T>(ref T backingField, T newValue, [CallerMemberName] string? propertyName = null)
    {
        // If they are equal, no action should be taken.
        if (EqualityComparer<T>.Default.Equals(backingField, newValue))
            return;

        if (propertyName is null)
            ArgumentNullException.ThrowIfNull(propertyName, nameof(propertyName));

        // Note(sewer): The fact events as simple as this allocate is a crime against humanity.
        // .NET should have had this be a struct reference.
        RaisePropertyChanging(new PropertyChangingEventArgs(propertyName));
        backingField = newValue;
        RaisePropertyChanged(new PropertyChangedEventArgs(propertyName));
    }
    
    /// <summary>
    /// A caller to ReactiveUI's <see cref="IReactiveObjectExtensions.RaiseAndSetIfChanged{TObj,TRet}"/>.
    /// I (Sewer) am not sure why this exists concretely, ask erri120.
    ///
    /// If you want to fire actual <see cref="PropertyChanged"/> event, invoke this class'
    /// <see cref="RaiseAndSetIfChanged{T}"/> instead.
    /// </summary>
    /// <param name="backingField"></param>
    /// <param name="newValue"></param>
    /// <param name="propertyName"></param>
    /// <typeparam name="T"></typeparam>
    protected void RaiseAndSetIfChangedReactive<T>(ref T backingField, T newValue, [CallerMemberName] string? propertyName = null)
    {
        // ReSharper disable once InvokeAsExtensionMethod
        ReactiveUI.IReactiveObjectExtensions.RaiseAndSetIfChanged(this, ref backingField, newValue, propertyName);
    }
}
