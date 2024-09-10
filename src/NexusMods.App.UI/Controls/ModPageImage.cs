using Avalonia.Media.Imaging;
using NexusMods.App.UI.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using SkiaSharp;
using BitFaster.Caching.Lru;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.NexusModsLibrary;

namespace NexusMods.App.UI.Controls;

public class ModPageImageCache
{
    private const int ImageWidth = 45;
    private const int ImageHeight = 28;
    private const int CacheCapacity = 100;

    private static readonly SKImageInfo SkImageInfo = new(
        width: ImageWidth,
        height: ImageHeight,
        colorType: SKImageInfo.PlatformColorType,
        alphaType: SKAlphaType.Opaque
    );

    // private readonly IConnection _connection;
    private readonly ConcurrentLru<EntityId, Bitmap> _cache = new(capacity: CacheCapacity);

    // public ModPageImageCache(IServiceProvider serviceProvider)
    // {
    //     _connection = serviceProvider.GetRequiredService<IConnection>();
    // }
    //
    // public async ValueTask<Bitmap> GetOrAddAsync(NexusModsModPageMetadataId id)
    // {
    //     return await _cache.GetOrAddAsync(id, static (id, self) =>
    //     {
    //         var modPage = NexusModsModPageMetadata.Load(self._connection.Db, id);
    //         if (!modPage.IsValid()) throw new NotImplementedException();
    //
    //         using var inputSkBitmap = SKBitmap.Decode(inputBytes);
    //         using var outputSkBitmap = inputSkBitmap.Resize(SkImageInfo, quality: SKFilterQuality.Low);
    //
    //         // TODO: save bytes
    //         // var outputBytes = outputSkBitmap.GetPixelSpan();
    //
    //         var avaloniaBitmap = outputSkBitmap.ToAvaloniaImage();
    //         return Task.FromResult(avaloniaBitmap);
    //     }, this);
    // }
}
