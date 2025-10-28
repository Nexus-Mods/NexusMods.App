using JetBrains.Annotations;

namespace NexusMods.Sdk.Settings;

/// <summary>
/// Represents a synchronous storage backend.
/// </summary>
/// <seealso cref="IAsyncStorageBackend"/>
[PublicAPI]
public interface IStorageBackend : IBaseStorageBackend, IDisposable
{
    /// <summary>
    /// Saves the given settings object to the storage backend synchronously.
    /// </summary>
    void Save<T>(T value, string? key) where T : class, ISettings, new();

    /// <summary>
    /// Loads the given settings type from the storage backend synchronously.
    /// </summary>
    /// <returns>
    /// Either the loaded value or <c>null</c> if the loading failed
    /// or the storage backend doesn't contain this value.
    /// </returns>
    T? Load<T>(string? key) where T : class, ISettings, new();
}
