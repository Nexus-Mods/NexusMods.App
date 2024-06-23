using System.Xml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Svg.Skia;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.IO;
using NexusMods.Hashing.xxHash64;
using Svg.Model;

namespace NexusMods.App.UI;

internal sealed class ImageCache : IImageCache
{
    private readonly ILogger<ImageCache> _logger;
    private readonly IFileStore _fileStore;
    private readonly HttpClient _client;

    private readonly Dictionary<Hash, IImage> _cache = new();

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

    private Task<IImage?> Load(ImageIdentifier imageIdentifier, CancellationToken cancellationToken)
    {
        return imageIdentifier.Union.Match(
            f0: uri => LoadFromUri(uri, cancellationToken),
            f1: hash => LoadFromHash(hash, cancellationToken)
        );
    }

    private async Task<IImage?> LoadFromUri(Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Fetching image from {Uri}", uri);
            var bytes = await _client.GetByteArrayAsync(uri, cancellationToken);
            var stream = new MemoryStream(bytes);
            return StreamToImage(stream);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while loading image from {Uri}", uri);
            return null;
        }
    }

    private async Task<IImage?> LoadFromHash(
        Hash hash,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await _fileStore.GetFileStream(hash, cancellationToken);
            return StreamToImage(stream);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while loading image from file store with hash {Hash}", hash);
            return null;
        }
    }

    private static IImage? StreamToImage(Stream stream)
    {
        return IsSvg(stream) ? FromSvg(stream) : new Bitmap(stream);
    }

    private static SvgImage? FromSvg(Stream stream)
    {
        var source = SvgSource.LoadFromStream(stream);
        var image = new SvgImage
        {
            Source = source,
        };

        return image;
    }

    private static bool IsSvg(Stream stream)
    {
        try
        {
            var firstByte = stream.ReadByte();
            if (firstByte != ('<' & 0xFF)) return false;

            stream.Seek(0, SeekOrigin.Begin);
            using var xmlReader = XmlReader.Create(stream);
            return xmlReader.MoveToContent() == XmlNodeType.Element && "svg".Equals(xmlReader.Name, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
        finally
        {
            stream.Seek(0, SeekOrigin.Begin);
        }
    }

    public void Dispose()
    {
        foreach (var kv in _cache)
        {
            var value = kv.Value;
            if (value is IDisposable disposable) disposable.Dispose();
        }
    }
}
