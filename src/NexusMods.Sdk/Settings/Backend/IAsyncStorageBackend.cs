using JetBrains.Annotations;

namespace NexusMods.Sdk.Settings;

/// <summary>
/// Represents an asynchronous storage backend.
/// </summary>
/// <seealso cref="IStorageBackend"/>
[PublicAPI]
public interface IAsyncStorageBackend : IBaseStorageBackend, IAsyncDisposable
{
    /// <summary>
    /// Saves the given settings object to the storage backend asynchronously.
    /// </summary>
    ValueTask Save<T>(T value, string? key, CancellationToken cancellationToken) where T : class, ISettings, new();

    /// <summary>
    /// Loads the given settings type from the storage backend asynchronously.
    /// </summary>
    /// <returns>
    /// Either the loaded value or <c>null</c> if the loading failed
    /// or the storage backend doesn't contain this value.
    /// </returns>
    ValueTask<T?> Load<T>(string? key, CancellationToken cancellationToken) where T : class, ISettings, new();
}
