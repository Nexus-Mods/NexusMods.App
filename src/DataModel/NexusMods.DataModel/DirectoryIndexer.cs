using NexusMods.DataModel.Abstractions;
using NexusMods.Paths;

namespace NexusMods.DataModel;

/// <summary>
/// The default directory indexer
/// </summary>
public class DirectoryIndexer : IDirectoryIndexer
{
    private readonly FileHashCache _hashCache;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="hashCache"></param>
    public DirectoryIndexer(FileHashCache hashCache)
    {
        _hashCache = hashCache;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<HashedEntry> IndexFolders(IEnumerable<AbsolutePath> paths, CancellationToken token = default)
    {
        return _hashCache.IndexFoldersAsync(paths, token);
    }
}
