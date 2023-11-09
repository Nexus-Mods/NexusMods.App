using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel;

/// <summary>
/// A stream factory that reads from the file store
/// </summary>
public class ArchiveManagerStreamFactory : IStreamFactory
{
    private readonly IFileStore _fileStore;
    private readonly Hash _hash;

    /// <summary>
    /// Main constructor
    /// </summary>
    /// <param name="fileStore"></param>
    /// <param name="hash"></param>
    public ArchiveManagerStreamFactory(IFileStore fileStore, Hash hash)
    {
        _fileStore = fileStore;
        _hash = hash;
    }

    /// <inheritdoc />
    public DateTime LastModifiedUtc { get; } = DateTime.UtcNow;

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
