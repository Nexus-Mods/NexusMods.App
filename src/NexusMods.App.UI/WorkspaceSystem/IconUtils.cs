using System.Diagnostics;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;
using Svg.Skia;

namespace NexusMods.App.UI.WorkspaceSystem;

internal static class IconUtils
{
    private const float IconSize = 18f;
    private const float CornerRadius = 1f;

    private const float OuterPaddingRatio = 0.08333333333333f;
    private const float OuterPadding = IconSize * OuterPaddingRatio;

    private const float InnerPaddingRatio = 0.14f;
    private const float InnerPadding = IconSize * InnerPaddingRatio;

    private static readonly SKColor BackgroundColor = SKColor.Parse("#101010");
    private static readonly SKColor FillColor = SKColor.Parse("#AAA");

    /// <summary>
    /// Generates a <see cref="Bitmap"/> for the given state.
    /// </summary>
    internal static Bitmap StateToBitmap(IReadOnlyDictionary<PanelId, Rect> state)
    {
        using var skPicture = GeneratePicture(state);
        using var skBitmap = skPicture.ToBitmap(
            background: BackgroundColor,
            scaleX: 10f,
            scaleY: 10f,
            skColorType: SKColorType.Bgra8888,
            skAlphaType: SKAlphaType.Premul,
            skColorSpace: SKColorSpace.CreateSrgb()
        );

        Debug.Assert(skBitmap is not null);
        var bitmap = new Bitmap(
            format: PixelFormat.Bgra8888,
            alphaFormat: AlphaFormat.Premul,
            data: skBitmap.GetPixels(),
            size: new PixelSize(skBitmap.Width, skBitmap.Height),
            dpi: new Vector(96.0, 96.0),
            stride: skBitmap.RowBytes
        );

        return bitmap;
    }

    private static SKPicture GeneratePicture(IReadOnlyDictionary<PanelId, Rect> state)
    {
        using var skPictureRecorder = new SKPictureRecorder();
        using var skCanvas = skPictureRecorder.BeginRecording(new SKRect(0f, 0f, IconSize, IconSize));

        // NOTE(erri120): There is probably a better way to do this, but this method works fine for now.
        // Essentially, we have a path for filled rectangles that is drawn using the fill color, and
        // a path for the "hollowed" rectangles that is drawn using the background color. The path
        // for the "hollowed" rectangles simply draws above the filled rectangles. There is probably
        // a way to do this using clipping or something.
        using var skPathFilled = new SKPath();
        using var skPathHollow = new SKPath();

        foreach (var kv in state)
        {
            var (panelId, rect) = kv;
            DrawRect(skPathFilled, skPathHollow, rect, isHollow: panelId != PanelId.Empty);
        }

        using (var skPaint = new SKPaint())
        {
            using var skShaderFilled = SKShader.CreateColor(
                FillColor,
                SKColorSpace.CreateRgb(SKColorSpaceTransferFn.Srgb, SKColorSpaceXyz.Srgb)
            );

            using var skShaderHollow = SKShader.CreateColor(
                BackgroundColor,
                SKColorSpace.CreateRgb(SKColorSpaceTransferFn.Srgb, SKColorSpaceXyz.Srgb)
            );

            skPaint.IsAntialias = true;

            // draw the filled rectangles
            skPaint.Shader = skShaderFilled;
            skCanvas.DrawPath(skPathFilled, skPaint);

            // draw the hollow rectangles
            skPaint.Shader = skShaderHollow;
            skCanvas.DrawPath(skPathHollow, skPaint);
        }

        var skPicture = skPictureRecorder.EndRecording();
        return skPicture;
    }

    private static void DrawRect(SKPath skPathFilled, SKPath skPathHollow, Rect rect, bool isHollow)
    {
        var x = (float)(rect.X * IconSize);
        var y = (float)(rect.Y * IconSize);
        var width = (float)(rect.Width * IconSize);
        var height = (float)(rect.Height * IconSize);

        var skRect = new SKRect(
            left: x + OuterPadding,
            right: x + width - OuterPadding,
            top: y + OuterPadding,
            bottom: y + height - OuterPadding
        );

        skPathFilled.AddRoundRect(skRect, rx: CornerRadius, ry: CornerRadius);
        if (!isHollow) return;

        skRect = new SKRect(
            left: x + InnerPadding,
            right: x + width - InnerPadding,
            top: y + InnerPadding,
            bottom: y + height - InnerPadding
        );

        skPathHollow.AddRect(skRect);
    }
}
