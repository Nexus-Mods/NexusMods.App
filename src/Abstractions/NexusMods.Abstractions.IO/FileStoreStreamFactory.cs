using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Abstractions.IO;

/// <summary>
/// A stream factory that reads from the file store
/// </summary>
public class FileStoreStreamFactory : IStreamFactory
{
    private readonly IFileStore _fileStore;
    private readonly Hash _hash;

    /// <summary>
    /// Main constructor
    /// </summary>
    /// <param name="fileStore"></param>
    /// <param name="hash"></param>
    public FileStoreStreamFactory(IFileStore fileStore, Hash hash)
    {
        _fileStore = fileStore;
        _hash = hash;
    }

    /// <inheritdoc />
    public required IPath Name { get; init;}

    /// <inheritdoc />
    public required Size Size { get; init; }

    /// <inheritdoc />
    public async ValueTask<Stream> GetStreamAsync()
    {
        return await _fileStore.GetFileStream(_hash);
    }
}
