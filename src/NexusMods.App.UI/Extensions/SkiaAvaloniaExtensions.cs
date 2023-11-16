using System.Diagnostics;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Skia;
using SkiaSharp;

namespace NexusMods.App.UI.Extensions;

/// <summary>
/// Extension methods for converting between Avalonia and Skia formats.
/// </summary>
public static class SkiaAvaloniaExtensions
{
    /// <summary>
    /// Converts an Avalonia <see cref="Bitmap"/> to a Skia <see cref="SKBitmap"/>.
    /// </summary>
    /// <remarks>
    /// This copies the pixels from <paramref name="bitmap"/>.
    /// </remarks>
    public static SKBitmap ToSkiaBitmap(this Bitmap bitmap)
    {
        var skBitmap = new SKBitmap(
            bitmap.PixelSize.Width,
            bitmap.PixelSize.Height,
            bitmap.Format?.ToSkColorType() ?? SKColorType.Bgra8888,
            SKAlphaType.Premul
        );

        var dest = skBitmap.GetPixels(out var length);
        Debug.Assert(dest != IntPtr.Zero);

        var bitsPerPixel = bitmap.Format?.BitsPerPixel ?? 4 * 8;
        var bytesPerPixel = bitsPerPixel >> 3;
        var stride = bitmap.PixelSize.Width * bytesPerPixel;

        bitmap.CopyPixels(new PixelRect(bitmap.PixelSize), dest, (int)length, stride);
        return skBitmap;
    }

    /// <summary>
    /// Converts a Skia <see cref="SKBitmap"/> to an Avalonia <see cref="Bitmap"/>.
    /// </summary>
    /// <remarks>
    /// This copies the pixels from <paramref name="skBitmap"/>.
    /// </remarks>
    public static Bitmap ToAvaloniaImage(this SKBitmap skBitmap)
    {
        var bitmap = new Bitmap(
            format: skBitmap.ColorType.ToPixelFormat(),
            alphaFormat: skBitmap.AlphaType.ToAlphaFormat(),
            data: skBitmap.GetPixels(),
            size: new PixelSize(skBitmap.Width, skBitmap.Height),
            dpi: new Vector(96.0, 96.0),
            stride: skBitmap.RowBytes
        );

        return bitmap;
    }
}
