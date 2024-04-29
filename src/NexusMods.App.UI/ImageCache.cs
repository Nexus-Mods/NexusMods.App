using Avalonia.Media;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.IO;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.App.UI;

internal sealed class ImageCache : IImageCache
{
    private readonly ILogger<ImageCache> _logger;
    private readonly IFileStore _fileStore;
    private readonly HttpClient _client;

    private readonly Dictionary<Hash, Bitmap> _cache = new();

    public ImageCache(
        ILogger<ImageCache> logger,
        IFileStore fileStore,
        HttpClient client)
    {
        _logger = logger;
        _fileStore = fileStore;
        _client = client;
    }

    public async Task<IImage?> GetImage(ImageIdentifier imageIdentifier, CancellationToken cancellationToken)
    {
        var hash = await Prefetch(imageIdentifier, cancellationToken);
        return hash == Hash.Zero ? null : _cache.GetValueOrDefault(hash);
    }

    public async Task<Hash> Prefetch(
        ImageIdentifier imageIdentifier,
        CancellationToken cancellationToken)
    {
        var hash = GetHash(imageIdentifier);
        if (_cache.TryGetValue(hash, out _)) return hash;

        var image = await Load(imageIdentifier, cancellationToken);
        if (image is null) return Hash.Zero;

        _cache.TryAdd(hash, image);
        return hash;
    }

    private static Hash GetHash(ImageIdentifier imageIdentifier)
    {
        return imageIdentifier.Union.Match(
            f0: uri => uri.ToString().XxHash64AsUtf8(),
            f1: hash => hash
        );
    }

    private Task<Bitmap?> Load(ImageIdentifier imageIdentifier, CancellationToken cancellationToken)
    {
        return imageIdentifier.Union.Match(
            f0: uri => LoadFromUri(uri, cancellationToken),
            f1: hash => LoadFromHash(hash, cancellationToken)
        );
    }

    private async Task<Bitmap?> LoadFromUri(Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Fetching image from {Uri}", uri);
            var stream = await _client.GetByteArrayAsync(uri, cancellationToken);
            return new Bitmap(new MemoryStream(stream));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while loading image from {Uri}", uri);
            return null;
        }
    }

    private async Task<Bitmap?> LoadFromHash(
        Hash hash,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await _fileStore.GetFileStream(hash, cancellationToken);
            var res = new Bitmap(stream);
            return res;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while loading image from file store with hash {Hash}", hash);
            return null;
        }
    }

    public void Dispose()
    {
        foreach (var kv in _cache)
        {
            kv.Value.Dispose();
        }
    }
}
