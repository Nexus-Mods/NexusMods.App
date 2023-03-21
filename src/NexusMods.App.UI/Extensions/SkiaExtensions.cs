using SkiaSharp;

namespace NexusMods.App.UI.Extensions;

/// <summary>
/// Extension methods related to Skia.
/// </summary>
public static class SkiaExtensions
{
    /// <summary>
    /// Blurs an image from the underlying Skia library.
    /// </summary>
    /// <param name="skiaImage">The image to blur.</param>
    /// <param name="sigmaX">Amount to blur in X direction.</param>
    /// <param name="sigmaY">Amount to blur in Y direction.</param>
    /// <returns>New image, which is a blurred version of the old image.</returns>
    public static SKBitmap BlurImage(this SKImage skiaImage, float sigmaX = 100f, float sigmaY = 100f)
    {
        var skiaInfo = new SKImageInfo(skiaImage.Width, skiaImage.Height, skiaImage.ColorType, skiaImage.AlphaType, skiaImage.ColorSpace);
        var renderTarget = SKImage.Create(skiaInfo);
        var bitmap = SKBitmap.FromImage(renderTarget);

        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint();

        paint.ImageFilter = SKImageFilter.CreateBlur(sigmaX, sigmaY);
        var src = SKRect.Create(0, 0, skiaImage.Width, skiaImage.Height);
        canvas.DrawImage(skiaImage, src, src, paint);

        return bitmap;
    }
}
