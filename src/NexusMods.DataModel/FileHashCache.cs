using System.Buffers.Binary;
using System.Text;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel;

public class FileHashCache
{
    private readonly ILogger<FileHashCache> _logger;
    private readonly IResource<FileHashCache, Size> _limiter;
    private readonly IDataStore _store;

    public FileHashCache(ILogger<FileHashCache> logger, IResource<FileHashCache, Size> limiter, IDataStore store)
    {
        _logger = logger;
        _limiter = limiter;
        _store = store;
    }

    public bool TryGetCached(AbsolutePath path, out FileHashCacheEntry entry)
    {
        var normalized = path.ToString();
        Span<byte> span = stackalloc byte[Encoding.UTF8.GetMaxByteCount(normalized.Length)];
        var used = Encoding.UTF8.GetBytes(normalized, span);
        var found = _store.GetRaw(span[..used], EntityCategory.FileHashes);
        if (found != null && found is not { Length: 0 })
        {
            entry = FileHashCacheEntry.FromSpan(found);
            return true;
        }
        entry = default;
        return false;
    }

    private void PutCachedAsync(AbsolutePath path, FileHashCacheEntry entry)
    {
        var normalized = path.ToString();
        Span<byte> kSpan = stackalloc byte[Encoding.UTF8.GetMaxByteCount(normalized.Length)];
        var used = Encoding.UTF8.GetBytes(normalized, kSpan);

        Span<byte> vSpan = stackalloc byte[24];
        entry.ToSpan(vSpan);

        _store.PutRaw(kSpan[..used], vSpan, EntityCategory.FileHashes);
    }

    public IAsyncEnumerable<HashedEntry> IndexFolder(AbsolutePath path, CancellationToken? token)
    {
        return IndexFolders(new[] { path }, token);
    }
    public async IAsyncEnumerable<HashedEntry> IndexFolders(IEnumerable<AbsolutePath> paths, CancellationToken? token)
    {
        token ??= CancellationToken.None;
        
        var result = _limiter.ForEachFile(paths, async (job, entry) =>
        {
            if (TryGetCached(entry.Path, out var found))
            {
                if (found.Size == entry.Size && found.LastModified == entry.LastModified)
                {
                    job.ReportNoWait(entry.Size);
                    return new HashedEntry(entry, found.Hash);
                }
            }

            var hashed = await entry.Path.XxHash64(token, job);
            PutCachedAsync(entry.Path, new FileHashCacheEntry(entry.LastModified, hashed, entry.Size));
            return new HashedEntry(entry, hashed);
        }, token, "Hashing Files");

        await foreach (var itm in result)
            yield return itm;
    }

    public async ValueTask<HashedEntry> HashFileAsync(AbsolutePath file, CancellationToken? token = null)
    {
        var info = file.FileInfo;
        if (TryGetCached(file, out var found))
        {
            if (found.Size == info.Length && found.LastModified == info.LastWriteTimeUtc)
            {
                return new HashedEntry(file, found.Hash, info.LastWriteTimeUtc, info.Length);
            }
        }

        using var job = await _limiter.Begin($"Hashing {file.FileName}", info.Length, token ?? CancellationToken.None);
        var hashed = await file.XxHash64(token, job);
        PutCachedAsync(file, new FileHashCacheEntry(info.LastWriteTimeUtc, hashed, info.Length));
        return new HashedEntry(file, hashed, info.LastWriteTimeUtc, info.Length);
    }
}

public record HashedEntry(AbsolutePath Path, Hash Hash, DateTime LastModified, Size Size) : FileEntry(Path, Size, LastModified)
{
    public HashedEntry(FileEntry fe, Hash hash) : this(fe.Path, hash, fe.LastModified, fe.Size){}

}


public readonly record struct FileHashCacheEntry(DateTime LastModified, Hash Hash, Size Size)
{
    public static FileHashCacheEntry FromSpan(ReadOnlySpan<byte> span)
    {
        var date = BinaryPrimitives.ReadInt64BigEndian(span);
        var hash = BinaryPrimitives.ReadUInt64BigEndian(span[8..]);
        var size = BinaryPrimitives.ReadInt64BigEndian(span[16..]);
        return new FileHashCacheEntry(DateTime.FromFileTimeUtc(date), Hash.FromULong(hash), size);
    }

    public void ToSpan(Span<byte> span)
    {
        BinaryPrimitives.WriteInt64BigEndian(span, LastModified.ToFileTimeUtc());
        BinaryPrimitives.WriteUInt64BigEndian(span[8..], (ulong)Hash);
        BinaryPrimitives.WriteInt64BigEndian(span[16..], Size);
    }
}