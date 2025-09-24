using JetBrains.Annotations;

namespace NexusMods.Sdk.Settings;

[PublicAPI]
public class StorageBackendOptions
{
    public bool IsDisabled => Id == StorageBackendId.DefaultValue;
    public required StorageBackendId Id { get; init; }

    public static readonly StorageBackendOptions Disable = new()
    {
        Id = StorageBackendId.DefaultValue,
    };

    public static StorageBackendOptions Use(StorageBackendId backendId) => new()
    {
        Id = backendId,
    };

    public static StorageBackendOptions Use<TBackend>(TBackend backend) where TBackend : IStorageBackend => new()
    {
        Id = backend.Id,
    };
}
