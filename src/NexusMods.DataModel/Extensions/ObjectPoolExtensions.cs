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
        return new(pool.Get(), pool);
    }

}

/// <summary>
/// Helper struct to wrap an object from an <see cref="ObjectPool{T}"/> and return it to the pool when disposed.
/// </summary>
/// <typeparam name="T"></typeparam>
public struct ObjectPoolDisposable<T> : IDisposable where T : class
{
    private readonly ObjectPool<T> _pool;
    public T Value { get; }

    public ObjectPoolDisposable(T value, ObjectPool<T> pool)
    {
        Value = value;
        _pool = pool;
    }

    public void Dispose()
    {
        _pool.Return(Value);
    }
}
