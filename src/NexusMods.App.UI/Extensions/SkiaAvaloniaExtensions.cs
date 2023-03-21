using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Skia;
using SkiaSharp;

namespace NexusMods.App.UI.Extensions;

/// <summary>
/// Extension classes related to Avalonia functionality.
/// </summary>
public static class SkiaAvaloniaExtensions
{
    /// <summary>
    /// Converts a Skia image to an avalonia image.
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
    /// Converts a Skia image to an avalonia image.
    /// </summary>
    /// <param name="bitmap">The bitmap to convert to Avalonia.</param>
    /// <returns>The blurred image.</returns>
    public static IBitmap ToAvaloniaImage(this SKBitmap bitmap)
    {
        // This is annoying, we convert to Avalonia for Avalonia to convert it back to Skia
        // but I can't find any docs on how to convert from Skia to Avalonia AT ALL,
        // so I'm just grokking this from walking the Avalonia sources.
        var info = bitmap.Info;
        var pixelFormat = info.ColorType.ToAvalonia();
        var alphaFormat = info.AlphaType.ToAlphaFormat();
        var pixelSize = new PixelSize(info.Width, info.Height);

        if (pixelFormat == null)
            throw new Exception("Not Supported Pixel Format");

        // Note: Don't use the other constructor, it does a copyBlock per stride/row, for some reason.
        return new Bitmap(pixelFormat.Value, alphaFormat, bitmap.GetPixels(), pixelSize, SkiaPlatform.DefaultDpi, bitmap.RowBytes);
    }
}
