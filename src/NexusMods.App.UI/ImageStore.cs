using System.Diagnostics;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Skia;
using BitFaster.Caching;
using JetBrains.Annotations;
using NexusMods.Abstractions.Media;
using NexusMods.MnemonicDB.Abstractions;
using OneOf;
using Size = NexusMods.Paths.Size;

namespace NexusMods.App.UI;

public sealed class ImageStore : IImageStore, IDisposable
{
    private readonly IConnection _connection;
    private SingletonCache<StoredImageId, Bitmap> _cache;

    public ImageStore(IConnection connection)
    {
        _connection = connection;
        _cache = new SingletonCache<StoredImageId, Bitmap>();
    }

    /// <inheritdoc/>
    public async ValueTask<StoredImage.ReadOnly> PutAsync(Bitmap bitmap)
    {
        using var tx = _connection.BeginTransaction();
        var storedImage = CreateStoredImage(tx, bitmap);

        var result = await tx.Commit();
        return result.Remap(storedImage);
    }

    /// <inheritdoc/>
    [MustDisposeResource] public Lifetime<Bitmap>? Get(OneOf<StoredImageId, StoredImage.ReadOnly> input)
    {
        if (input.TryPickT0(out var id, out var storedImage))
        {
            storedImage = StoredImage.Load(_connection.Db, id);
        }

        if (!storedImage.IsValid()) return null;
        var metadata = storedImage.Metadata;
        var bytes = storedImage.ImageData.Data;

        Debug.Assert((ulong)bytes.Length == metadata.DataLength);
        var lifetime = _cache.Acquire(id, _ => ToBitmap(metadata, bytes));
        return lifetime;
    }

    /// <inheritdoc/>
    StoredImage.New IImageStore.CreateStoredImage(ITransaction transaction, Bitmap bitmap) => CreateStoredImage(transaction, bitmap);

    public static StoredImage.New CreateStoredImage(ITransaction transaction, Bitmap bitmap)
    {
        var metadata = ToMetadata(bitmap);
        var bytes = GC.AllocateUninitializedArray<byte>(length: (int)metadata.DataLength);
        GetBitmapBytes(metadata, bitmap, bytes);

        var imageData = CompressData(metadata, bytes);

        var storedImage = new StoredImage.New(transaction)
        {
            Metadata = metadata,
            ImageData = imageData,
        };

        return storedImage;
    }

    private static ImageData CompressData(ImageMetadata metadata, byte[] uncompressedData)
    {
        // TODO: optional compression for larger images
        return new ImageData(ImageDataCompression.None, uncompressedData);
    }

    private static void GetBitmapBytes(ImageMetadata metadata, Bitmap bitmap, byte[] bytes)
    {
        unsafe
        {
            fixed (byte* b = bytes)
            {
                var ptr = new IntPtr(b);
                bitmap.CopyPixels(
                    sourceRect: new PixelRect(metadata.PixelSize),
                    buffer: ptr,
                    bufferSize: (int)metadata.DataLength,
                    stride: metadata.Stride
                );
            }
        }
    }

    private static Bitmap ToBitmap(ImageMetadata metadata, byte[] bytes)
    {
        unsafe
        {
            fixed (byte* b = bytes)
            {
                var ptr = new IntPtr(b);
                var bitmap = new Bitmap(
                    format: metadata.PixelFormat,
                    alphaFormat: metadata.AlphaFormat,
                    data: ptr,
                    size: metadata.PixelSize,
                    dpi: new Vector(metadata.Dpi, metadata.Dpi),
                    stride: metadata.Stride
                );

                return bitmap;
            }
        }
    }

    private static ImageMetadata ToMetadata(Bitmap bitmap)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bitmap.PixelSize.Width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bitmap.PixelSize.Height);

        var width = (uint)bitmap.PixelSize.Width;
        var height = (uint)bitmap.PixelSize.Height;

        if (!bitmap.Format.HasValue) throw new NotSupportedException("Bitmap doesn't have a PixelFormat");
        var format = bitmap.Format.Value;

        if (format.BitsPerPixel % 8 != 0) throw new NotSupportedException($"Format `{format}` isn't supported");

        if (!bitmap.AlphaFormat.HasValue) throw new NotSupportedException("Bitmap doesn't have an AlphaFormat");
        var alphaFormat = bitmap.AlphaFormat.Value;

        var dpi = bitmap.Dpi;
        var x = (int)Math.Floor(dpi.X);
        var y = (int)Math.Floor(dpi.Y);
        if (x != y) throw new NotSupportedException($"Uneven DPI isn't supported: `{dpi.ToString()}`");

        // NOTE(erri120): small hack, Avalonia PixelFormat struct is sealed, and we can't really do anything with it.
        // Instead, we'll just convert to SkColorType using the method provided by Avalonia.
        var skColorType = format.ToSkColorType();
        skColorType.ToPixelFormat();

        var metadata = new ImageMetadata(
            imageWidth: width,
            imageHeight: height,
            skColorType: skColorType,
            alphaFormat: alphaFormat,
            dpi: (uint)x
        );

        // 16MB is enough to store a RAW image of 2000x2000 with 4 bytes per pixel
        // 8.3MB is enough to store a RAW image of 1920x1080 with 4 bytes per pixel
        var maxSize = Size.MB * 16;

        if (Size.From(metadata.DataLength) > maxSize)
            throw new NotSupportedException($"Large images above `{maxSize}` aren't supported!");

        return metadata;
    }

    private bool _isDisposed;
    public void Dispose()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        _cache = null!;
        _isDisposed = true;
    }
}

