using Microsoft.Extensions.ObjectPool;

namespace NexusMods.DataModel.Extensions;

public static class ObjectPoolExtensions
{
    /// <summary>
    /// Gets an object from the pool and wraps it in a <see cref="ObjectPoolDisposable{T}"/> that will return it to the pool when disposed.
    /// </summary>
    /// <param name="pool"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static ObjectPoolDisposable<T> RentDisposable<T>(this ObjectPool<T> pool) where T : class
    {
        return new ObjectPoolDisposable<T>(pool.Get(), pool);
    }

}

/// <summary>
/// Helper struct to wrap an object from an <see cref="ObjectPool{T}"/> and return it to the pool when disposed.
/// </summary>
/// <typeparam name="T"></typeparam>
public struct ObjectPoolDisposable<T> : IDisposable where T : class
{
    private bool _isDisposed;

    private readonly ObjectPool<T> _pool;
    private T _value;

    public T Value
    {
        get
        {
            if (_isDisposed)
                throw new ObjectDisposedException(typeof(ObjectPoolDisposable<>).Name);
            return _value;
        }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="pool"></param>
    public ObjectPoolDisposable(T value, ObjectPool<T> pool)
    {
        _value = value;
        _pool = pool;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed) return;
        _pool.Return(Value);
        _value = null!;
        _isDisposed = true;
    }
}
