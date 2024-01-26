using System.Diagnostics;
using JetBrains.Annotations;

namespace NexusMods.Networking.NexusWebApi.Auth;

/// <summary>
/// Simple reference value object cache with a lifetime.
/// </summary>
[PublicAPI]
public struct CachedObject<T> where T : class
{
    private T? _object;
    private DateTimeOffset _expiredAt;
    private readonly TimeSpan _lifetime;

    /// <summary>
    /// Constructor
    /// </summary>
    public CachedObject(TimeSpan lifetime)
    {
        _expiredAt = DateTimeOffset.MinValue;
        _lifetime = lifetime;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public CachedObject(T? initialValue, TimeSpan lifetime)
    {
        _lifetime = lifetime;
        Store(initialValue);
    }

    /// <summary>
    /// Returns <c>true</c> if the cache has expired.
    /// </summary>
    public bool HasExpired() => _expiredAt - TimeSpan.FromSeconds(1) < DateTimeOffset.UtcNow;

    /// <summary>
    /// Returns the stored object or <c>null</c> if the cache has expired.
    /// </summary>
    public T? Get()
    {
        if (_object is null) return null;
        return HasExpired() ? null : _object;
    }

    /// <summary>
    /// Stores an object in the cache and updates the expiration date.
    /// </summary>
    public void Store(T? obj)
    {
        Debug.Assert(obj is not IDisposable and not IAsyncDisposable, "Disposable objects can't be stored in the cache.");
        _object = obj;
        _expiredAt = DateTimeOffset.UtcNow + _lifetime;
    }

    /// <summary>
    /// Immediately expires the current cache.
    /// </summary>
    public void Evict()
    {
        _object = null;
        _expiredAt = DateTimeOffset.UtcNow;
    }
}
