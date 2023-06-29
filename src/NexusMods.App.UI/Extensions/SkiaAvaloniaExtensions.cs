using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;

namespace NexusMods.App.UI.Extensions;

/// <summary>
/// Extension classes related to Avalonia functionality.
/// </summary>
public static class SkiaAvaloniaExtensions
{
    /// <summary>
    /// Converts an Avalonia writeable bitmap to a Skia image.
    /// </summary>
    /// <param name="writeable">The bitmap to convert to Avalonia.</param>
    /// <returns>The blurred image.</returns>
    public static SKImage ToSkiaImage(this WriteableBitmap writeable)
    {
        // See: ToAvaloniaImage
        using var locked = writeable.Lock();
        var size = writeable.PixelSize;
        var imageInfo = new SKImageInfo(size.Width, size.Height, locked.Format.ToSkColorType());
        return SKImage.FromPixels(imageInfo, locked.Address);
    }

    /// <summary>
    /// Converts an Avalonia bitmap to a Skia image.
    /// </summary>
    /// <param name="bitmap">The bitmap to convert to Avalonia.</param>
    /// <returns>The blurred image.</returns>
    public static SKImage ToSkiaImage(this Bitmap bitmap)
    {
        var info = new SKImageInfo(bitmap.PixelSize.Width, bitmap.PixelSize.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        var result = new SKBitmap(info);
        var dest = result.GetPixels(out var length);
        var stride = bitmap.PixelSize.Width * 4;
        bitmap.CopyPixels(new PixelRect(bitmap.PixelSize), dest, (int)length, stride);
        return SKImage.FromBitmap(result);
    }

    /// <summary>
    /// Converts a Skia image to an avalonia image.
    /// </summary>
    /// <param name="bitmap">The bitmap to convert to Avalonia.</param>
    /// <returns>The blurred image.</returns>
    public static Bitmap ToAvaloniaImage(this SKBitmap bitmap)
    {
        return new Bitmap(PixelFormat.Bgra8888,
            AlphaFormat.Premul,
            bitmap.GetPixels(),
            new PixelSize(bitmap.Width, bitmap.Height),
            new Vector(96.0, 96.0),
            bitmap.RowBytes);
    }
}
