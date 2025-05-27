using Microsoft.Extensions.Logging;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Abstractions.IO;

/// <summary>
/// A file store implementation that aggregates multiple readable and writable backend stores.
/// </summary>
public class AggregateFileStore : IFileStore
{
    private readonly AsyncFriendlyReaderWriterLock _lock = new();
    private readonly List<IReadableFileStoreBackend> _readableBackends;
    private readonly IWriteableFileStoreBackend _writableBackend;

    public AggregateFileStore(ILogger<AggregateFileStore> store, IEnumerable<IReadableFileStoreBackend> backends)
    {
        _readableBackends = backends.ToList();
        _writableBackend = _readableBackends.OfType<IWriteableFileStoreBackend>().Single();
    }


    /// <inheritdoc />
    public async ValueTask<bool> HaveFile(Hash hash)
    {
        foreach (var backend in _readableBackends)
        {
            if (await backend.HaveFile(hash))
                return true;
        }
        return false;
    }

    public async Task<Stream> GetFileStream(Hash hash, CancellationToken token = default)
    {
        foreach (var backend in _readableBackends)
        {
            if (await backend.HaveFile(hash))
                return await backend.GetFileStream(hash, token);
        }
        throw new FileNotFoundException($"File with hash {hash} not found in any backend.");
    }

    public Task BackupFiles(IEnumerable<ArchivedFileEntry> backups, bool deduplicate = true, CancellationToken token = default)
    {
        return _writableBackend.BackupFiles(backups, deduplicate, token);
    }

    public async Task ExtractFiles(IEnumerable<(Hash Hash, AbsolutePath Dest)> files, CancellationToken token = default)
    {
        await Parallel.ForEachAsync(files, token, async (pair, itoken) =>
            {
                var (hash, dest) = pair;
                // Ensure the destination directory exists
                dest.Parent.CreateDirectory();

                // Get the file stream and copy it to the destination
                try
                {
                    await using var stream = await GetFileStream(hash, itoken);
                    await using var fileStream = dest.Create();
                    await stream.CopyToAsync(fileStream, itoken);
                }
                catch (Exception)
                {
                    if (dest.FileExists)
                        dest.Delete();
                    throw;
                }
            }
        );
    }

    public async Task<Dictionary<Hash, byte[]>> ExtractFiles(IEnumerable<Hash> files, CancellationToken token = default)
    {
        var dest = new Dictionary<Hash, byte[]>();
        await Parallel.ForEachAsync(files, token, async (hash, itoken) =>
            {

                // Get the file stream and copy it to the destination
                await using var stream = await GetFileStream(hash, itoken);
                await using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream, itoken);
                lock (dest)
                {
                    dest[hash] = memoryStream.ToArray();
                }
            }
        );
        return dest;
    }

    /// <inheritdoc />
    public async Task<byte[]> Load(Hash hash, CancellationToken token = default)
    {
        await using var stream = await GetFileStream(hash, token);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, token);
        return memoryStream.ToArray();
    }

    public AsyncFriendlyReaderWriterLock.WriteLockDisposable Lock() => _lock.WriteLock();
}
