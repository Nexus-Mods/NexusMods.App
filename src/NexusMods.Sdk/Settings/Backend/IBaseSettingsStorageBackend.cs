using JetBrains.Annotations;

namespace NexusMods.Sdk.Settings;

[PublicAPI]
public interface IStorageBackendDefinition
{
    static abstract StorageBackendId Id { get; }
}

/// <summary>
/// Base interface for storage backends.
/// </summary>
[PublicAPI]
public interface IBaseStorageBackend
{
    /// <summary>
    /// Unique identifier of this backend.
    /// </summary>
    StorageBackendId Id { get; }
}
