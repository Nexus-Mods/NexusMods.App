using NexusMods.DataModel.Abstractions;
using NexusMods.Paths;

namespace NexusMods.DataModel;

public class DirectoryIndexer : IDirectoryIndexer
{
    private readonly FileHashCache _hashCache;

    public DirectoryIndexer(FileHashCache hashCache)
    {
        _hashCache = hashCache;
    }
    public IAsyncEnumerable<HashedEntry> IndexFolders(IEnumerable<AbsolutePath> paths, CancellationToken token = default)
    {
        return _hashCache.IndexFoldersAsync(paths, token);
    }
}
