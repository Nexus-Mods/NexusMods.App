using JetBrains.Annotations;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Base interface for storage backends.
/// </summary>
[PublicAPI]
[Obsolete("Replaced with new settings API")]
public interface IBaseSettingsStorageBackend
{
    /// <summary>
    /// Unique identifier of this backend.
    /// </summary>
    SettingsStorageBackendId Id { get; }
}

/// <summary>
/// Represents a synchronous storage backend.
/// </summary>
/// <seealso cref="IAsyncSettingsStorageBackend"/>
[PublicAPI]
[Obsolete("Replaced with new settings API")]
public interface ISettingsStorageBackend : IBaseSettingsStorageBackend, IDisposable
{
    /// <summary>
    /// Saves the given settings object to the storage backend synchronously.
    /// </summary>
    void Save<T>(T value) where T : class, ISettings, new();

    /// <summary>
    /// Loads the given settings type from the storage backend synchronously.
    /// </summary>
    /// <returns>
    /// Either the loaded value or <c>null</c> if the loading failed
    /// or the storage backend doesn't contain this value.
    /// </returns>
    T? Load<T>() where T : class, ISettings, new();
}

/// <summary>
/// Represents an asynchronous storage backend.
/// </summary>
/// <seealso cref="ISettingsStorageBackend"/>
[PublicAPI]
[Obsolete("Replaced with new settings API")]
public interface IAsyncSettingsStorageBackend : IBaseSettingsStorageBackend, IAsyncDisposable
{
    /// <summary>
    /// Saves the given settings object to the storage backend asynchronously.
    /// </summary>
    ValueTask Save<T>(T value, CancellationToken cancellationToken) where T : class, ISettings, new();

    /// <summary>
    /// Loads the given settings type from the storage backend asynchronously.
    /// </summary>
    /// <returns>
    /// Either the loaded value or <c>null</c> if the loading failed
    /// or the storage backend doesn't contain this value.
    /// </returns>
    ValueTask<T?> Load<T>(CancellationToken cancellationToken) where T : class, ISettings, new();
}
