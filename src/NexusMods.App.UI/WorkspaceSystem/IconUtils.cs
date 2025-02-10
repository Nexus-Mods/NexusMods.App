using System.Diagnostics;
using Avalonia;
using Avalonia.Media.Imaging;
using NexusMods.App.UI.Extensions;
using SkiaSharp;
using Svg.Skia;

namespace NexusMods.App.UI.WorkspaceSystem;

internal static class IconUtils
{
    private const float IconSize = 30f;
    private const float CornerRadius = 2f;

    private const float OuterPadding = 1.25f;
    private const float InnerPadding = OuterPadding + 2.5f;

    private const float Scale = 5f;

    private static readonly SKColor BackgroundColor = SKColor.Parse("#1D1D21");
    private static readonly SKColor FillColor = SKColor.Parse("#D4D4D8");

    /// <summary>
    /// Generates a <see cref="Bitmap"/> for the given state.
    /// </summary>
    internal static Bitmap StateToBitmap(WorkspaceGridState state)
    {
        using var skPicture = GeneratePicture(state);
        using var skBitmap = skPicture.ToBitmap(
            background: BackgroundColor,
            scaleX: Scale,
            scaleY: Scale,
            skColorType: SKColorType.Bgra8888,
            skAlphaType: SKAlphaType.Premul,
            skColorSpace: SKColorSpace.CreateSrgb()
        );

        Debug.Assert(skBitmap is not null);
        return skBitmap.ToAvaloniaImage();
    }

    private static SKPicture GeneratePicture(WorkspaceGridState state)
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

        foreach (var panel in state)
        {
            var (panelId, rect) = panel;
            DrawRect(skPathFilled, skPathHollow, rect, isHollow: panelId != PanelId.DefaultValue);
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
