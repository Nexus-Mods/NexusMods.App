namespace NexusMods.App.UI.Helpers;

/// <summary>
/// A wrapper that ties the lifetime of an external object with an <see cref="IObservable{T}"/>.
/// This allows an external resource, such as a 'wrapper' or 'adapter' to be disposed when the wrapped observable is disposed.
/// </summary>
/// <remarks>
/// Use this if you want to use an adapter or wrapper around an observable and want to dispose the adapter/wrapper with it.
/// 
/// <code>
/// // Where you normally would return 'observable' but want to wrap it.
/// var adapter = new SomeAdapter(_connection, observable);
/// return new DisposableObservableWrapper(adapter.WrappedObservable(), adapter);
/// </code>
/// </remarks>
/// <typeparam name="T">The type of the elements in the sequence.</typeparam>
public class DisposableObservableWrapper<T>(IObservable<T> source, IDisposable additionalResource) : IObservable<T>, IDisposable
{
    private readonly IObservable<T> _source = source ?? throw new ArgumentNullException(nameof(source));
    private readonly IDisposable _additionalResource = additionalResource ?? throw new ArgumentNullException(nameof(additionalResource));
    private bool _disposed;

    /// <inheritdoc />
    public IDisposable Subscribe(IObserver<T> observer)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(DisposableObservableWrapper<T>));
        return _source.Subscribe(observer);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
            
        // Dispose the additional resource when this wrapper is disposed
        _additionalResource.Dispose();
            
        // If the source is also IDisposable, dispose it too
        if (_source is IDisposable disposableSource)
            disposableSource.Dispose();
    }
}
