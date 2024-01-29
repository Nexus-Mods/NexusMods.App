using NexusMods.Paths;

namespace NexusMods.Abstractions.DiskState;

/// <summary>
/// Component that indexes a directory and returns the files and folders as <see cref="HashedEntry"/>s.
/// </summary>
public interface IDirectoryIndexer
{
    /// <summary>
    /// Indexes the given paths and returns the files and folders as <see cref="HashedEntry"/>s.
    /// </summary>
    /// <param name="paths"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public IAsyncEnumerable<KeyValuePair<AbsolutePath, DiskStateEntry>> IndexFolders(IEnumerable<AbsolutePath> paths,
        CancellationToken token = default);
}
