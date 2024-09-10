using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;

namespace NexusMods.App.UI;

[StructLayout(LayoutKind.Sequential)]
public readonly struct ImageMetadata
{
    public readonly uint ImageWidth;
    public readonly uint ImageHeight;
    public readonly SKColorType SkColorType;
    public readonly AlphaFormat AlphaFormat;
    public readonly uint Dpi;

    public PixelSize PixelSize => new((int)ImageWidth, (int)ImageHeight);
    public PixelFormat PixelFormat => SkColorType.ToPixelFormat();

    // NOTE(erri120): Going from bits to bytes requires dividing by 8, aka bit shift by 3
    public int Stride => ((int)ImageWidth * PixelFormat.BitsPerPixel) >> 3;
    public ulong DataLength => (ImageWidth * ImageHeight * (uint)PixelFormat.BitsPerPixel) >> 3;

    public ImageMetadata(uint imageWidth, uint imageHeight, SKColorType skColorType, AlphaFormat alphaFormat, uint dpi)
    {
        ImageWidth = imageWidth;
        ImageHeight = imageHeight;
        SkColorType = skColorType;
        AlphaFormat = alphaFormat;
        Dpi = dpi;

        Guard(skColorType);
    }

    public static ImageMetadata Read(ReadOnlySpan<byte> bytes)
    {
        Debug.Assert(bytes.Length == Marshal.SizeOf<ImageMetadata>());

        unsafe
        {
            fixed (byte* b = bytes)
            {
                return Unsafe.Read<ImageMetadata>(b);
            }
        }
    }

    public void Write(Span<byte> bytes)
    {
        Debug.Assert(bytes.Length == Marshal.SizeOf<ImageMetadata>());

        unsafe
        {
            fixed (void* b = bytes)
            {
                Unsafe.Write(b, this);
            }
        }
    }

    private static void Guard(SKColorType skColorType)
    {
        // NOTE(erri120): safe-guard, this should never be hit in the real world, only odd formats like greyscale images
        // would trigger this.
        if (skColorType.ToPixelFormat().BitsPerPixel % 8 == 0) return;
        ThrowHelper(skColorType);
    }

    [DoesNotReturn]
    private static void ThrowHelper(SKColorType skColorType)
    {
        throw new NotSupportedException($"BitsPerPixel of `{skColorType.ToPixelFormat()}` is `{skColorType.ToPixelFormat().BitsPerPixel}` and not divisible by 8!");
    }
}
