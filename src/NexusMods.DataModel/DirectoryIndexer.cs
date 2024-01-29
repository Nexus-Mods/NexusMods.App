using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Paths;

namespace NexusMods.DataModel;

/// <summary>
/// The default directory indexer
/// </summary>
public class DirectoryIndexer : IDirectoryIndexer
{
    private readonly IFileHashCache _hashCache;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="hashCache"></param>
    public DirectoryIndexer(IFileHashCache hashCache)
    {
        _hashCache = hashCache;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<HashedEntry> IndexFolders(IEnumerable<AbsolutePath> paths, CancellationToken token = default)
    {
        return _hashCache.IndexFoldersAsync(paths, token);
    }
}
