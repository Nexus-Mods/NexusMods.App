using NexusMods.Abstractions.IO;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Benchmarks.Benchmarks.Loadouts.Harness;

public class DummyFileStore : IFileStore
{
    public ValueTask<bool> HaveFile(Hash hash)
    {
        return ValueTask.FromResult(false);
    }

    public Task BackupFiles(IEnumerable<ArchivedFileEntry> backups, bool deduplicate = true, CancellationToken token = default)
    {
        return Task.CompletedTask;
    }

    public Task BackupFiles(string archiveName, IEnumerable<ArchivedFileEntry> files, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task ExtractFiles(IEnumerable<(Hash Hash, AbsolutePath Dest)> files, CancellationToken token = default)
    {
        return Task.CompletedTask;
    }

    public Task<Dictionary<Hash, byte[]>> ExtractFiles(IEnumerable<Hash> files, CancellationToken token = default)
    {
        return Task.FromResult(new Dictionary<Hash, byte[]>());
    }

    public Task<Stream> GetFileStream(Hash hash, CancellationToken token = default)
    {
        return null!;
    }

    public HashSet<ulong> GetFileHashes()
    {
        return [];
    }

    public AsyncFriendlyReaderWriterLock.WriteLockDisposable Lock() => throw new NotImplementedException(); // Only used by GC, so never called right now.
}
