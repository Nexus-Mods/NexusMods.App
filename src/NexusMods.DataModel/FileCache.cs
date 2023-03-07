using Microsoft.Extensions.Logging;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel;

public class FileCache
{
    private readonly ILogger<FileCache> _logger;
    private readonly AbsolutePath _cachePath;

    public FileCache(ILogger<FileCache> logger, AbsolutePath cachePath)
    {
        _logger = logger;
        _cachePath = cachePath;
        if (!cachePath.DirectoryExists())
            cachePath.CreateDirectory();
    }

    public async ValueTask<CacheEntry> Create()
    {
        var guid = Guid.NewGuid();
        var path = _cachePath.CombineUnchecked(guid.ToString());

        return new CacheEntry(this, path);
    }

    public async Task<Stream?> Read(Hash hash)
    {
        var path = HashPath(hash);
        return path.FileExists ? path.Read() : null;
    }

    private AbsolutePath HashPath(Hash hash)
    {
        return _cachePath.CombineUnchecked(hash.ToString());
    }

    public async Task<bool> CopyTo(Hash hash, AbsolutePath destination, CancellationToken token = default)
    {
        var path = HashPath(hash);
        if (!path.FileExists) return false;
        await path.CopyToAsync(destination, token);
        return true;
    }

    private async Task<Hash> Finish(CacheEntry cacheEntry)
    {
        var hash = await cacheEntry.Path.XxHash64();
        await cacheEntry.Path.MoveToAsync(_cachePath.CombineUnchecked(hash.ToString()));
        return hash;
    }

    public class CacheEntry : IAsyncDisposable
    {
        private bool _isValid = true;
        private bool _isDisposed = false;
        private readonly FileCache _cache;
        private Hash _hash;
        public AbsolutePath Path { get; }

        public CacheEntry(FileCache cache, AbsolutePath path)
        {
            _cache = cache;
            Path = path;
        }

        public void DontCache()
        {
            _isValid = false;
        }

        public async ValueTask DisposeAsync()
        {
            _isDisposed = true;
            if (_isValid)
                _hash = await _cache.Finish(this);
        }
    }


}
