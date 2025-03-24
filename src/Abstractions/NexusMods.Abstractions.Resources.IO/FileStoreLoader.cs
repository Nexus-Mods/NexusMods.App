using JetBrains.Annotations;
using NexusMods.Abstractions.IO;
using NexusMods.Hashing.xxHash3;

namespace NexusMods.Abstractions.Resources.IO;

[PublicAPI]
public sealed class FileStoreLoader : IResourceLoader<Hash, byte[]>
{
    private readonly IFileStore _fileStore;

    /// <summary>
    /// Constructor.
    /// </summary>
    public FileStoreLoader(IFileStore fileStore)
    {
        _fileStore = fileStore;
    }

    /// <inheritdoc/>
    public async ValueTask<Resource<byte[]>> LoadResourceAsync(Hash resourceIdentifier, CancellationToken cancellationToken)
    {
        await using var stream = await _fileStore.GetFileStream(resourceIdentifier, cancellationToken);

        var bytes = GC.AllocateUninitializedArray<byte>(length: (int)stream.Length);
        var totalBytesRead = 0;
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(bytes.AsMemory(start: totalBytesRead), cancellationToken).ConfigureAwait(false)) != 0)
        {
            totalBytesRead += bytesRead;
        }

        ArgumentOutOfRangeException.ThrowIfNotEqual(totalBytesRead, bytes.Length);
        return new Resource<byte[]>
        {
            Data = bytes,
        };
    }
}
