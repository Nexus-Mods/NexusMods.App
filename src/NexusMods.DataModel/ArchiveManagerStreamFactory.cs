using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel;

/// <summary>
/// A stream factory that reads from the archive manager
/// </summary>
public class ArchiveManagerStreamFactory : IStreamFactory
{
    private readonly IArchiveManager _archiveManager;
    private readonly Hash _hash;

    /// <summary>
    /// Main constructor
    /// </summary>
    /// <param name="archiveManager"></param>
    /// <param name="hash"></param>
    public ArchiveManagerStreamFactory(IArchiveManager archiveManager, Hash hash)
    {
        _archiveManager = archiveManager;
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
        return await _archiveManager.GetFileStream(_hash);
    }
}
