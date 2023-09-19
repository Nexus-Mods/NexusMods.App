using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using NexusMods.Common.GuidedInstaller;
using NexusMods.DataModel.Abstractions;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.App.UI;

internal sealed class ImageCache : IImageCache
{
    private readonly ILogger<ImageCache> _logger;
    private readonly IArchiveManager _archiveManager;

    private readonly Dictionary<Hash, Bitmap> _cache = new();

    public ImageCache(
        ILogger<ImageCache> logger,
        IArchiveManager archiveManager)
    {
        _logger = logger;
        _archiveManager = archiveManager;
    }

    public async Task<IImage?> GetImage(OptionImage optionImage, CancellationToken cancellationToken)
    {
        var hash = GetHash(optionImage);
        if (_cache.TryGetValue(hash, out var cachedImage)) return cachedImage;

        var image = await Load(optionImage, cancellationToken);
        if (image is null) return null;

        _cache.TryAdd(hash, image);
        return image;
    }

    private static Hash GetHash(OptionImage optionImage)
    {
        if (optionImage.IsT0) return optionImage.AsT0.ToString().XxHash64AsUtf8();
        if (optionImage.IsT1) return optionImage.AsT1.FileHash;
        throw new UnreachableException();
    }

    private Task<Bitmap?> Load(OptionImage optionImage, CancellationToken cancellationToken)
    {
        if (optionImage.IsT0) return LoadRemoteImage(optionImage.AsT0, cancellationToken);
        if (optionImage.IsT1) return LoadImageFromArchive(optionImage.AsT1, cancellationToken);
        throw new UnreachableException();
    }

    private async Task<Bitmap?> LoadRemoteImage(Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            var client = new HttpClient();
            var stream = await client.GetByteArrayAsync(uri, cancellationToken);
            return new Bitmap(new MemoryStream(stream));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while loading image from {Uri}", uri);
            return null;
        }
    }

    private async Task<Bitmap?> LoadImageFromArchive(
        OptionImage.ImageFromArchive imageFromArchive,
        CancellationToken cancellationToken)
    {
        await using var stream = await _archiveManager.GetFileStream(imageFromArchive.FileHash, cancellationToken);
        var res = new Bitmap(stream);
        return res;
    }

    public void Dispose()
    {
        foreach (var kv in _cache)
        {
            kv.Value.Dispose();
        }
    }
}
