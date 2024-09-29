using JetBrains.Annotations;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Resources;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel;

[PublicAPI]
public class FileStoreLoader : IResourceLoader<Hash, byte[]>
{
    private readonly IFileStore _fileStore;

    public FileStoreLoader(IFileStore fileStore)
    {
        _fileStore = fileStore;
    }

    public async ValueTask<Resource<byte[]>> LoadResourceAsync(Hash resourceIdentifier, CancellationToken cancellationToken)
    {
        await using var stream = await _fileStore.GetFileStream(resourceIdentifier, cancellationToken);

        var bytes = GC.AllocateUninitializedArray<byte>(length: (int)stream.Length);
        var count = await stream.ReadAsync(bytes, cancellationToken: cancellationToken);

        ArgumentOutOfRangeException.ThrowIfNotEqual(count, bytes.Length);
        return new Resource<byte[]>
        {
            Data = bytes,
        };
    }
}
