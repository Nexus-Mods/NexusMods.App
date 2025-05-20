using JetBrains.Annotations;
using NexusMods.Abstractions.IO;
using NexusMods.Hashing.xxHash3;
using NexusMods.Sdk.Resources;

namespace NexusMods.Abstractions.Resources.IO;


/// <summary>
/// Resource loader that returns a stream of a file in the file store.
/// </summary>
[PublicAPI]
public sealed class FileStoreStreamLoader : IResourceLoader<Hash, Stream>
{
    private readonly IFileStore _fileStore;
    
    /// <summary>
    /// Constructor.
    /// </summary>
    public FileStoreStreamLoader(IFileStore fileStore)
    {
        _fileStore = fileStore;
    }
    
    /// <inheritdoc/>
    public async ValueTask<Resource<Stream>> LoadResourceAsync(Hash resourceIdentifier, CancellationToken cancellationToken)
    {
        var stream = await _fileStore.GetFileStream(resourceIdentifier, cancellationToken);
        return new Resource<Stream>
        {
            Data = stream,
        };
    }
}
